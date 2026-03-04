using System.Collections.Generic;
using System.Threading.Tasks;
using TWCF_BibleApp.Application.Interfaces;
using TWCF_BibleApp.Domain;

namespace TWCF_BibleApp.Application.Services
{
    public sealed class UserService
    {
        private readonly IUserRepository _repo;

        public UserService(IUserRepository repo)
        {
            _repo = repo;
        }

        public Task<UserSetting?> GetUserSettingAsync() => _repo.GetUserSettingAsync();
        public Task SaveUserSettingAsync(UserSetting setting) => _repo.SaveUserSettingAsync(setting);

        public Task<IEnumerable<Note>> GetNotesAsync(int bookId, int chapterNumber) => _repo.GetNotesAsync(bookId, chapterNumber);
        public Task AddNoteAsync(Note note) => _repo.AddNoteAsync(note);
        public Task UpdateNoteAsync(Note note) => _repo.UpdateNoteAsync(note);
        public Task DeleteNoteAsync(int noteId) => _repo.DeleteNoteAsync(noteId);

        public Task<IEnumerable<Highlight>> GetHighlightsAsync(int bookId, int chapterNumber) => _repo.GetHighlightsAsync(bookId, chapterNumber);
        public Task AddHighlightAsync(Highlight highlight) => _repo.AddHighlightAsync(highlight);
        public Task DeleteHighlightAsync(int highlightId) => _repo.DeleteHighlightAsync(highlightId);
    }
}
