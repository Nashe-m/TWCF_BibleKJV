TWCF Bible App

A clean, modern .NET MAUI Bible Reader built with layered architecture, search functionality, and structured domain modeling.

Built with maintainability, scalability, and clean architecture principles in mind.


Features

📚 Bible reader with book & chapter navigation

🔎 Full-text verse search

🎯 Direct navigation to selected verse

🧱 Clean layered architecture (Domain-driven structure)

💾 SQLite persistence

🧭 Shell-based navigation

🎨 Smooth fade animations on chapter change

🏗 Architecture

The application follows a structured multi-layer architecture:

TWCF_BibleApp
│
├── Domain              → Core entities (Verse, Book)
├── Application         → Interfaces, DTOs, business logic contracts
├── Persistence         → EF Core, SQLite, repository implementations
├── Presentation        → ViewModels & MAUI Views
├── Platforms           → Platform-specific code
Design Patterns Used

Repository Pattern

MVVM Pattern

Dependency Injection

Clean Separation of Concerns


🛠 Tech Stack

.NET MAUI

C#

Entity Framework Core

SQLite

Shell Navigation

MVVM Architecture

📦 Installation
1️⃣ Clone the repository
git clone https://github.com/Nashe-m/TWCF_BibleApp.git
cd TWCF_BibleApp
2️⃣ Restore dependencies
dotnet restore
3️⃣ Run the project
dotnet build
dotnet run

Or open in Visual Studio 2022+ and run on:

Windows

Android Emulator

Physical Android Device



🔎Search Feature

The app includes verse-level search functionality:

Case-sensitive text matching

Sorted by book order → chapter → verse

Tap result → navigates directly to that verse in the Reader



🎯 Project Goals

Build a structured, maintainable Bible application

Practice clean architecture principles in MAUI

Implement feature growth incrementally (Search → Highlights → Bookmarks → Notes)

Prepare for possible Play Store deployment



🔮 Future Improvements

Highlight verses

Bookmark system

Notes per verse

Multiple Bible translations

Improved search indexing

Dark mode refinement

Performance optimization

👨‍💻 Author

Developed by Nashe M
