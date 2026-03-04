using System;

namespace TWCF_BibleApp.Domain
{
    public sealed class Translation
    {
        public int Id { get; init; }
        public string Code { get; init; } = string.Empty; // e.g. "KJV"
        public string Name { get; init; } = string.Empty; // e.g. "King James Version"

        public Translation() { }

        public Translation(int id, string code, string name)
        {
            if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required", nameof(code));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required", nameof(name));

            Id = id;
            Code = code;
            Name = name;
        }
    }
}
