"""
Builds Resources/Raw/bible_data.db from eBible.org USFM downloads.

Usage:
    python Tools/import_usfm.py <usfm_root> <books_chapters_source.db> <output.db>
    python Tools/import_usfm.py --finalize <existing.db>

<usfm_root> must contain one unzipped folder per translation, named by its eBible id
(see TRANSLATIONS below), e.g. <usfm_root>/eng-kjv2006/*.usfm.
Download from https://ebible.org/Scriptures/<id>_usfm.zip

Books and Chapters are copied unchanged from the source database so existing
BookIds (1..66) stay stable for the user's highlights, notes and settings.

Words of Jesus (USFM \\wj ... \\wj*) are kept in Verses.MarkedText as <wj>...</wj>.
Verses.Text is always plain text (used for search and copy).

--finalize re-applies the last step (testament fix-up and search index) to an existing
database without re-importing the text.
"""
import re
import sqlite3
import sys
from collections import Counter
from pathlib import Path

# Id is stable and stored in user settings - never renumber.
TRANSLATIONS = [
    (1, "eng-kjv2006", "KJV", "King James Version", "English",
     "Public domain."),
    (2, "engwebp", "WEB", "World English Bible", "English",
     "Public domain. \"World English Bible\" is a trademark of eBible.org."),
    (3, "eng-asv", "ASV", "American Standard Version", "English",
     "Public domain."),
    (4, "sna", "SNA", "Shona Contemporary Bible (2017)", "Shona",
     "Copyright \u00a9 2017 Biblica, Inc. Licensed under the Creative Commons "
     "Attribution-ShareAlike 4.0 license (CC BY-SA 4.0). Source: open.bible / eBible.org."),
]

BOOK_CODES = [
    "GEN", "EXO", "LEV", "NUM", "DEU", "JOS", "JDG", "RUT", "1SA", "2SA", "1KI", "2KI",
    "1CH", "2CH", "EZR", "NEH", "EST", "JOB", "PSA", "PRO", "ECC", "SNG", "ISA", "JER",
    "LAM", "EZK", "DAN", "HOS", "JOL", "AMO", "OBA", "JON", "MIC", "NAM", "HAB", "ZEP",
    "HAG", "ZEC", "MAL",
    "MAT", "MRK", "LUK", "JHN", "ACT", "ROM", "1CO", "2CO", "GAL", "EPH", "PHP", "COL",
    "1TH", "2TH", "1TI", "2TI", "TIT", "PHM", "HEB", "JAS", "1PE", "2PE", "1JN", "2JN",
    "3JN", "JUD", "REV",
]
BOOK_IDS = {code: i + 1 for i, code in enumerate(BOOK_CODES)}

# Paragraph-level markers whose line content is not verse text (titles, headings,
# Psalm superscriptions, chapter labels, ...).
SKIP_LINE = re.compile(
    r"^(id|ide|h|qa|toc\d*|toca\d*|mt\d*|mte\d*|ms\d*|mr|s\d*|sr|r|d|cl|cp|cd|rem|sts|"
    r"restore|imt\d*|is\d*|ip|ipi|im|imi|ipq|imq|ipr|iq\d*|ib|ili\d*|iot|io\d*|ior|"
    r"iex|imte\d*|ie|periph|sp)$"
)

PARAGRAPH_RE = re.compile(r"^(p|m|po|pr|pc|pm|pmo|pmc|pmr|pi\d*|mi|nb|b|q\d*|qr|qc|qm\d*|qd|"
                          r"li\d*|lh|lf|lim\d*|ph\d*|tr|th\d*|tc\d*|thr\d*|tcr\d*)$")
NOTE_RE =re.compile(r"\\(f|fe|ef|x|ex|fig)\s.*?\\\1\*", re.S)
WORD_RE = re.compile(r"\\(\+?w)\s+([^|\\]*?)(?:\|[^\\]*?)?\\\1\*")
# An opening marker owns the single space after it; a closing marker (\nd*) does not.
MARKER_RE = re.compile(r"\\(\+?[a-z]+\d*)(\*|\s?)")
VERSE_RE = re.compile(r"\\v\s+(\d+)[a-z]?(?:-\d+[a-z]?)?\s*")


def clean_inline(s):
    """Turn inline USFM into plain text, keeping \\wj as <wj> tags."""
    def repl(m):
        tag, closing = m.group(1).lstrip("+"), m.group(2) == "*"
        if tag == "wj":
            return "</wj>" if closing else "<wj>"
        # Any other character/paragraph marker: drop it, keep its content.
        return " " if not closing and PARAGRAPH_RE.match(tag) else ""
    return MARKER_RE.sub(repl, s)


def normalise(marked):
    marked = marked.replace("\u00b6", "")  # KJV pilcrows
    marked = re.sub(r"\s+", " ", marked)
    # Keep spaces outside the tags and drop empty pairs.
    marked = re.sub(r"<wj>\s+", " <wj>", marked)
    marked = re.sub(r"\s+</wj>", "</wj> ", marked)
    marked = re.sub(r"<wj></wj>", "", marked)
    marked = re.sub(r"</wj>(\s*)<wj>", r"\1", marked)
    marked = re.sub(r"\s+([,.;:!?\u201d\u2019)])", r"\1", marked)
    return re.sub(r"\s+", " ", marked).strip()


def balance(marked, open_at_start):
    """Close a <wj> left open at the end of a verse; report whether it carries over."""
    if open_at_start:
        marked = "<wj>" + marked
    depth = 0
    for m in re.finditer(r"</?wj>", marked):
        depth = 0 if m.group(0) == "</wj>" else 1
    if depth:
        marked += "</wj>"
    return marked, bool(depth)


def parse_book(path):
    """Yield (chapter, verse, marked_text) for one USFM book file."""
    text = path.read_text(encoding="utf-8-sig")
    text = NOTE_RE.sub("", text)
    text = WORD_RE.sub(r"\2", text)

    chapter, verse, buf = 0, 0, []
    in_wj = False

    def flush():
        nonlocal in_wj
        if chapter and verse and buf:
            marked, in_wj = balance(clean_inline("".join(buf)), in_wj)
            marked = normalise(marked)
            if marked:
                return (chapter, verse, marked)
        return None

    for raw in text.splitlines():
        line = raw.strip()
        if not line.startswith("\\"):
            if verse:
                buf.append(" " + line)
            continue
        m = re.match(r"\\(\S+?)(\*?)(?:\s+|$)(.*)", line)
        marker, rest = m.group(1), m.group(3)
        if marker == "c":
            row = flush()
            if row:
                yield row
            chapter, verse, buf = int(rest.split()[0]), 0, []
            continue
        if SKIP_LINE.match(marker):
            continue

        # Paragraph markers (\p, \q1, \li1, ...) are line breaks; anything else
        # (\v, \wj, \nd, ...) is part of the verse text and parsed inline.
        content = " " + rest if PARAGRAPH_RE.match(marker) else line
        parts = VERSE_RE.split(content)
        # parts = [before_first_v, vnum, text, vnum, text, ...]
        if verse:
            buf.append(parts[0])
        for i in range(1, len(parts), 2):
            row = flush()
            if row:
                yield row
            verse, buf = int(parts[i]), [parts[i + 1]]
    row = flush()
    if row:
        yield row


def main(usfm_root, source_db, out_db):
    usfm_root, out_db = Path(usfm_root), Path(out_db)
    if out_db.exists():
        out_db.unlink()

    src = sqlite3.connect(source_db)
    db = sqlite3.connect(out_db)
    db.executescript("""
        CREATE TABLE Translations (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Code TEXT NOT NULL,
            Name TEXT NOT NULL,
            Language TEXT NOT NULL DEFAULT '',
            Copyright TEXT NOT NULL DEFAULT '',
            HasWordsOfJesus INTEGER NOT NULL DEFAULT 0
        );
        CREATE TABLE Books (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            OrderNumber INTEGER NOT NULL,
            Testament TEXT NOT NULL
        );
        CREATE TABLE Chapters (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            BookId INTEGER NOT NULL,
            Number INTEGER NOT NULL,
            VerseCount INTEGER NOT NULL
        );
        CREATE TABLE Verses (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            BookId INTEGER NOT NULL,
            ChapterNumber INTEGER NOT NULL,
            VerseNumber INTEGER NOT NULL,
            Text TEXT NOT NULL,
            MarkedText TEXT NULL,
            TranslationId INTEGER NOT NULL
        );
    """)
    db.executemany("INSERT INTO Books VALUES (?,?,?,?)",
                   src.execute("SELECT Id, Name, OrderNumber, Testament FROM Books"))
    db.executemany("INSERT INTO Chapters VALUES (?,?,?,?)",
                   src.execute("SELECT Id, BookId, Number, VerseCount FROM Chapters"))
    expected = {(b, c): n for b, c, n in db.execute("SELECT BookId, Number, VerseCount FROM Chapters")}

    for tid, folder, code, name, lang, copyright_ in TRANSLATIONS:
        rows, seen = [], set()
        for f in sorted((usfm_root / folder).glob("*.usfm")):
            book_code = re.match(r"\d+-([0-9A-Z]{3})", f.name).group(1)
            book_id = BOOK_IDS.get(book_code)
            if not book_id:
                continue  # front matter, glossary, deuterocanon
            for ch, v, marked in parse_book(f):
                if (book_id, ch, v) in seen:
                    continue
                seen.add((book_id, ch, v))
                plain = re.sub(r"</?wj>", "", marked)
                rows.append((book_id, ch, v, plain, marked if "<wj>" in marked else None, tid))

        db.executemany("INSERT INTO Verses (BookId, ChapterNumber, VerseNumber, Text, MarkedText, TranslationId) "
                       "VALUES (?,?,?,?,?,?)", rows)
        has_wj = int(any(r[4] for r in rows))
        db.execute("INSERT INTO Translations VALUES (?,?,?,?,?,?)", (tid, code, name, lang, copyright_, has_wj))

        per_chapter = Counter((r[0], r[1]) for r in rows)
        missing = sorted(k for k in expected if k not in per_chapter)
        differ = sorted(k for k in expected if k in per_chapter and per_chapter[k] != expected[k])
        print(f"{code}: {len(rows)} verses, {sum(1 for r in rows if r[4])} with words of Jesus, "
              f"{len(missing)} chapters missing, {len(differ)} chapters with a different verse count")
        if missing:
            print("   missing:", missing[:10])
        if differ:
            print("   differ:", [(k, per_chapter[k], expected[k]) for k in differ[:10]])

    db.executescript("""
        CREATE INDEX IX_Verses_Translation_Book_Chapter ON Verses (TranslationId, BookId, ChapterNumber, VerseNumber);
        CREATE INDEX IX_Chapters_BookId ON Chapters (BookId, Number);
    """)
    finalize(db)
    db.close()


def finalize(db):
    """Fix up book testaments and (re)build the full-text search index."""
    # The source database had several NT books (1 Corinthians, 1 Peter, Revelation, ...)
    # marked OT. Matthew is book 40, so derive it from the order instead of trusting it.
    db.execute("UPDATE Books SET Testament = CASE WHEN OrderNumber >= 40 THEN 'NT' ELSE 'OT' END")

    # External-content FTS5 index over Verses.Text; rowid = Verses.Id. Used by
    # BibleRepository.SearchVersesAsync. remove_diacritics lets "e" match "é" etc.
    db.executescript("""
        DROP TABLE IF EXISTS VerseSearch;
        CREATE VIRTUAL TABLE VerseSearch USING fts5(
            Text, content='Verses', content_rowid='Id', tokenize='unicode61 remove_diacritics 2');
        INSERT INTO VerseSearch(VerseSearch) VALUES ('rebuild');
        INSERT INTO VerseSearch(VerseSearch) VALUES ('optimize');
    """)
    db.commit()
    db.execute("VACUUM")
    print("finalized:", db.execute("SELECT Testament, COUNT(*) FROM Books GROUP BY 1").fetchall())


if __name__ == "__main__":
    if len(sys.argv) == 3 and sys.argv[1] == "--finalize":
        conn = sqlite3.connect(sys.argv[2])
        finalize(conn)
        conn.close()
    elif len(sys.argv) == 4:
        main(*sys.argv[1:])
    else:
        sys.exit(__doc__)
