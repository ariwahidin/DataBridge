# DataBridge — Migration & Run Guide

## 5. Setup Connection String

Buka appsettings.json, ganti YOUR_SERVER dengan nama SQL Server kamu:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=DataBridgeConfig;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Contoh kalau pakai local SQL Server Express:
```
Server=.\SQLEXPRESS;Database=DataBridgeConfig;Trusted_Connection=True;TrustServerCertificate=True;
```

---

## 6. Jalankan Migration

Buka **Tools → NuGet Package Manager → Package Manager Console**:

```
Add-Migration InitialCreate
Update-Database
```

Ini akan:
- Buat database `DataBridgeConfig` otomatis
- Buat tabel `Users`
- Insert default admin user: `admin` / `Admin@123`

---

## 7. Jalankan Aplikasi

Tekan **F5** atau klik tombol Run di Visual Studio.

Browser akan otomatis membuka halaman Login.

---

## 8. Default Credentials

| Field    | Value      |
|----------|------------|
| Username | admin      |
| Password | Admin@123  |

> **Ganti password setelah login pertama!**

---

## 9. Struktur File yang Perlu Dibuat

Pastikan semua file ini sudah ada di project:

```
DataBridge/
├── Models/
│   ├── Enums/
│   │   └── UserRole.cs
│   ├── Entities/
│   │   └── User.cs
│   └── ViewModels/
│       └── Auth/
│           └── LoginViewModel.cs
├── Data/
│   └── AppDbContext.cs
├── Repositories/
│   ├── Interfaces/
│   │   └── IUserRepository.cs
│   └── EF/
│       └── UserRepository.cs
├── Services/
│   └── AuthService.cs
├── Middleware/
│   └── AuthMiddleware.cs
├── Controllers/
│   ├── AuthController.cs
│   └── DashboardController.cs
├── Views/
│   ├── Auth/
│   │   └── Login.cshtml
│   └── Dashboard/
│       └── Index.cshtml
├── appsettings.json
└── Program.cs
```

---

## Catatan

- Session timeout: 8 jam (bisa diubah di Program.cs)
- Password di-hash pakai BCrypt (cost factor default = 11)
- Semua route otomatis di-protect oleh AuthMiddleware
- Route publik hanya: /Auth/Login dan /Auth/Logout
