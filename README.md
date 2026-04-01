# DataBridge Starter Kit

Enterprise ASP.NET Core MVC starter with:
- Session-based authentication (BCrypt)
- Role-based access control (Admin / Viewer)
- Clean light theme layout (sidebar, topbar, responsive)
- EF Core + Dapper hybrid
- SQL Server

## Stack
- ASP.NET Core 8 MVC
- Entity Framework Core 8
- Dapper
- Quartz.NET (scheduler)
- ClosedXML (Excel export)
- MailKit (email)

## Getting Started
1. Clone repo
2. Copy `appsettings.example.json` → `appsettings.json`, isi connection string
3. Jalankan migration: `Update-Database`
4. Run project
5. Login: `admin` / `Admin@123`
