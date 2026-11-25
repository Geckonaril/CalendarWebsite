# Xiaomi Takvim Web Uygulaması

BLM4581 Ağ Tabanlı Teknolojiler ve Uygulamaları
dersi için geliştirilen takvim yönetim sistemi.

## Teknolojiler

- ASP.NET Core MVC (.NET 10)
- ASP.NET Core Web API
- PostgreSQL 18
- Entity Framework Core
- HTML, CSS, JavaScript, Bootstrap

## Proje Yapısı

- src/Takvim.Web: Web arayüzü
- src/Takvim.Api: REST API
- Takvim.slnx: Çözüm dosyası

## Geliştirme Ortamı

- Windows 11
- WSL 2 / Ubuntu
- .NET 10 SDK
- PostgreSQL 18

## Çalıştırma

Web uygulaması:

cd src/Takvim.Web
dotnet run

API uygulaması:

cd src/Takvim.Api
dotnet run

## Veritabanı

PostgreSQL üzerinde takvim_db veritabanı kullanılır.

Bağlantı bilgileri .NET User Secrets ile saklanır.

## Etkinlik API'si (4. Hafta)

Etkinlikler için RESTful Web API (`/api/events`), JWT Bearer ile korunur.
Kullanıcı yalnızca kendi takvimlerindeki etkinliklere erişebilir.

| Yöntem | Uç nokta | Açıklama |
|--------|----------|----------|
| GET | /api/events?from=&to= | Etkinlikleri listeler (isteğe bağlı UTC tarih aralığı) |
| GET | /api/events/{id} | Tek etkinlik |
| POST | /api/events | Etkinlik oluşturur (takvim verilmezse varsayılan "Takvimim" kullanılır) |
| PUT | /api/events/{id} | Etkinliği günceller |
| DELETE | /api/events/{id} | Etkinliği siler |

### Swagger ile test

1. API'yi çalıştırın ve http://localhost:5055/swagger adresini açın.
2. `POST /api/account/login` ile giriş yapın, yanıttaki `accessToken` değerini kopyalayın.
3. Sağ üstteki **Authorize** düğmesine token'ı yapıştırın; ardından Events uç noktalarını deneyin.

Alternatif olarak `src/Takvim.Api/Takvim.Api.http` dosyasındaki hazır istekler kullanılabilir.

### JWT yapılandırması

`Jwt:Key` geliştirme ortamında `appsettings.Development.json` içinde tanımlıdır.
Üretim ortamında bu anahtar mutlaka User Secrets veya ortam değişkeni ile değiştirilmelidir:

    dotnet user-secrets set "Jwt:Key" "<en-az-32-karakterlik-gizli-anahtar>"

Web uygulaması girişte aldığı token'ı oturum çerezinde saklar ve API isteklerine
otomatik olarak ekler. Web arayüzünde etkinlik ekleme, listeleme, güncelleme ve
silme işlemleri **Takvimim** (aylık takvim) ve **Etkinliklerim** sayfalarından yapılır.

## Planlanan Özellikler

- Kullanıcı kayıt ve giriş işlemleri
- Günlük, haftalık ve aylık takvim
- Kategori ve hatırlatıcı yönetimi
- Yönetici paneli
- Rol bazlı yetkilendirme


## Haftalık Geliştirme Süreci

### Hafta 1 — Proje Kurulumu ve Geliştirme Ortamı

- .NET 10 SDK ve ASP.NET Core geliştirme ortamı hazırlandı.
- ASP.NET Core MVC (`Takvim.Web`) ve Web API (`Takvim.Api`) projeleri oluşturuldu.
- Projeler `Takvim.slnx` çözüm dosyası altında birleştirildi.
- PostgreSQL 18 üzerinde `takvim_db` veritabanı oluşturuldu.
- API ile PostgreSQL arasındaki bağlantı yapılandırıldı.
- Veritabanı bağlantı bilgileri .NET User Secrets ile güvenli şekilde saklandı.
- `/api/health/database` endpoint'i geliştirilerek veritabanı bağlantısı test edildi.
- Bootstrap kullanılarak temel web arayüzü oluşturuldu.
- GitHub deposu, `.gitignore` ve başlangıç README dosyası hazırlandı.

### Hafta 2 — Veritabanı Tasarımı ve Takvim Arayüzü

- PostgreSQL için beş temel veri modeli oluşturuldu:
  - `Users`: Kullanıcı bilgileri
  - `Calendars`: Kullanıcı takvimleri
  - `Events`: Takvim etkinlikleri
  - `Categories`: Etkinlik kategorileri
  - `Reminders`: Etkinlik hatırlatıcıları
- Entity Framework Core kullanılarak `AppDbContext` sınıfı oluşturuldu.
- Tablolar arasında bire-çok ilişkiler ve foreign key bağlantıları tanımlandı.
- `InitialCreate` migration oluşturuldu ve PostgreSQL veritabanına uygulandı.
- MVC üzerinde aylık takvim arayüzü geliştirildi.
- Önceki/sonraki aylar arasında geçiş, bugüne dönme ve gün seçme özellikleri eklendi.
- JavaScript ile dinamik takvim oluşturma işlemleri gerçekleştirildi.
- Takvim arayüzü farklı ekran boyutlarına uyumlu hâle getirildi.

### Hafta 3 — Kimlik Doğrulama, Yetkilendirme ve Arayüz Geliştirme

- ASP.NET Core Identity altyapısı projeye entegre edildi.
- Kullanıcı modeli `IdentityUser<int>` tabanlı yapıya dönüştürüldü.
- Identity veritabanı tabloları için `AddIdentity` migration işlemleri gerçekleştirildi.
- Kullanıcı kayıt ve giriş işlemleri için REST API endpoint'leri geliştirildi:
  - `POST /api/account/register`
  - `POST /api/account/login`
- Kullanıcı parolalarının Identity üzerinden güvenli şekilde hashlenmesi sağlandı.
- Parola uzunluğu, büyük/küçük harf ve rakam gereksinimleri uygulandı.
- MVC tarafında cookie tabanlı oturum yönetimi gerçekleştirildi.
- Kullanıcı giriş, kayıt ve çıkış işlemleri web arayüzüne entegre edildi.
- `Admin` ve `User` rolleri tanımlandı.
- `[Authorize]` ve `[Authorize(Roles = "Admin")]` kullanılarak sayfa erişim kontrolleri eklendi.
- Normal kullanıcıların yönetici sayfasına erişimi engellendi ve yetkisiz erişim ekranı oluşturuldu.
- Geliştirme ortamında yönetici hesabı oluşturmak için User Secrets tabanlı yapılandırma eklendi.
- Takvim+ arayüzü modern ve responsive bir tasarımla yenilendi.
- Sol navigasyon menüsü, aylık takvim, seçili gün paneli ve giriş/kayıt ekranları yeniden tasarlandı.
- Form doğrulamaları, hatalı parola ve kullanıcı oturum testleri gerçekleştirildi.

### Hafta 4 — Planlanan Geliştirmeler

- Etkinlik yönetimi için RESTful Web API CRUD endpoint'lerinin geliştirilmesi.
- Etkinlik oluşturma, listeleme, güncelleme ve silme işlemlerinin tamamlanması.
- API endpoint'lerinin Swagger/OpenAPI üzerinden test edilmesi.
- Etkinlik yönetiminin Takvim+ web arayüzüne entegre edilmesi.
- Kullanıcıların yalnızca kendilerine ait etkinlikleri yönetebilmesi için erişim kontrollerinin uygulanması.