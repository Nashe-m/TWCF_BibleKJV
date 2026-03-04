Preparation steps to create and bundle `bible_data.db` (KJV) for the app

1. Source KJV data
   - Obtain a public domain KJV text in a structured format (CSV, SQLite dump, or XML). There are public datasets available (e.g., DigitalBiblePlatform dumps or open KJV imports).

2. Create `bible_data.db` (SQLite)
   - Create tables matching the persistence entities: `Translations`, `Books`, `Chapters`, `Verses`.
   - Example DDL:
     ```sql
     CREATE TABLE Translations (Id INTEGER PRIMARY KEY, Code TEXT NOT NULL, Name TEXT NOT NULL);
     CREATE TABLE Books (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, [Order] INTEGER NOT NULL, Testament INTEGER NOT NULL);
     CREATE TABLE Chapters (Id INTEGER PRIMARY KEY, BookId INTEGER NOT NULL, Number INTEGER NOT NULL, VerseCount INTEGER NOT NULL);
     CREATE TABLE Verses (Id INTEGER PRIMARY KEY, TranslationId INTEGER NOT NULL, BookId INTEGER NOT NULL, ChapterNumber INTEGER NOT NULL, VerseNumber INTEGER NOT NULL, Text TEXT NOT NULL);
     ```

3. Import KJV content
   - Map verses to the `Verses` table with `TranslationId = 1` for KJV.
   - Ensure indexes on `(BookId, ChapterNumber)` and `(BookId, ChapterNumber, VerseNumber)`.

4. Validation
   - Confirm that `SELECT COUNT(*) FROM Verses WHERE TranslationId = 1;` matches expected verse count (~31,102 verses depending on counting method).

5. Bundle with app
   - Place `bible_data.db` into the MAUI project's `Resources/Raw` folder so it's packaged with the app.
   - Add `<MauiAsset Include="Resources/Raw/bible_data.db" />` in the csproj if necessary.

6. First-run copy
   - On first run, copy the bundled `bible_data.db` to `FileSystem.AppDataDirectory` and open it read-only using the `BibleDataContext` connection string with `Mode=ReadOnly`.

7. Notes
   - Keep `bible_data.db` immutable at runtime; do not run migrations against it.
   - Ensure `Id` values for `Translation` (KJV) are stable (use `Id = 1`).

