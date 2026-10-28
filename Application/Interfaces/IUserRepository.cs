using System.Collections.Generic;
using System.Threading.Tasks;
using TWCF_BibleApp.Domain;

namespace TWCF_BibleApp.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<UserSetting?> GetUserSettingAsync();
        Task SaveUserSettingAsync(UserSetting setting);

        Task<IEnumerable<Note>> GetNotesAsync(int bookId, int chapterNumber);
        /// <summary>Saves a new note and returns it with its Id.</summary>
        Task<Note> AddNoteAsync(Note note);
        Task UpdateNoteAsync(Note note);
        Task DeleteNoteAsync(int noteId);

        Task<IEnumerable<Highlight>> GetHighlightsAsync(int bookId, int chapterNumber);
        /// <summary>Saves a new highlight and returns it with its Id.</summary>
        Task<Highlight> AddHighlightAsync(Highlight highlight);
        Task DeleteHighlightAsync(int highlightId);
    }
}
