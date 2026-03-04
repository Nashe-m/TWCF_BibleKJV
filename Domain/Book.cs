using System;
using TWCF_BibleApp.Domain.Enums;

namespace TWCF_BibleApp.Domain
{
    public sealed class Book
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public int OrderNumber { get; init; }
        public Testament Testament { get; init; }

        public Book() { }

        public Book(int id, string name, int order, Testament testament)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required", nameof(name));

            Id = id;
            Name = name;
            OrderNumber = order;
            Testament = testament;
        }
    }
}
