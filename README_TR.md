$$\large\color{green}\textbf{**Durum Güncellemesi (29/08/2026):** 2.0.1 ve 2.0.2 Sürümleri ÇALIŞIYOR!}$$

[![C#](https://img.shields.io/badge/Language-C%23-512BD4?style=for-the-badge&logo=csharp&logoColor=white)](https://dotnet.microsoft.com/)
[![Framework](https://img.shields.io/badge/Framework-.NET%206.0%2B-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download)
[![Release](https://img.shields.io/badge/Release-v2.0.2-brightgreen?style=for-the-badge&logo=github)](https://github.com/s0rp/CSharpSpotiLyrics/releases)
[![License](https://img.shields.io/badge/License-MIT-blue?style=for-the-badge)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%7C%20macOS-lightgrey?style=for-the-badge)]()

### 🌐 Canlı Demo (proof-of-concept)

Bu kütüphanenin canlı ortamda nasıl çalıştığını deneyimlemek için kişisel web sitemi ziyaret edebilirsiniz:
👉 **[sxrp.me](https://sxrp.me)**

> **Nasıl Çalışıyor?**
> Bu web sitesi, arka planda **CSharpSpotiLyrics** kütüphanesini kullanarak (LRPC-API projesi private ama altyapısında en son sürüm CSharpSpotiLyrics yatıyor) Spotify hesabımda o anda çalmakta olan şarkıyı, player durumunu ve senkronize şarkı sözlerini 7/24 gerçek zamanlı (real-time) olarak çekip dinamik bir arayüzle sergilemektedir (Durgunsa muhtemelen uyuyorumdur :D)

### AI Asistanlarında prompt verirken kullanmak İçin (Claude/ChatGPT/Cursor vb): 
[GitIngest](https://gitingest.com/s0rp/CSharpSpotiLyrics)

## 📚 Dokümantasyon:
[İnteraktif Dökümantasyon](https://s0rp.github.io/CSharpSpotiLyrics/)
[AI & LLM Friendly Docs (llms.txt)](https://s0rp.github.io/CSharpSpotiLyrics/llms.txt)


# CSharpSpotiLyrics
> **Spotify'dan senkronize şarkı sözlerini (`.lrc`) indirmek için C# ile geliştirilmiş profesyonel bir komut satırı aracı (CLI).**

**CSharpSpotiLyrics**; tekil parçalar, albümler, çalma listeleri, o anda aktif olarak çalan şarkınız veya doğrudan kayıtlı kütüphanenizden interaktif olarak seçeceğiniz içerikler için şarkı sözlerini indirebilen ultra hafif bir kütüphanedir. Ayrıca yerel müzik klasörünüzdeki ses dosyalarını tarayıp meta verilerine (ID3) göre Spotify üzerinde otomatik eşleştirme yaparak şarkı sözü indirebilme özelliğine de sahiptir.

---

## Önizleme & Ekran Görüntüleri

### Terminal Çalışması & LRC Çıktısı
![CLI Execution](Images/cmd_WpIiihAIV9.png)

### İndirilen LRC Dosyaları & Klasör Yapısı
![Downloaded Files](Images/explorer_mcq6yd1ogN.png)
![Album LRC Folder](Images/explorer_6F0ZA3YEjm.png)

### CLI Seçenekleri & Yardım Menüsü
![CLI Help](Images/cmd_D5Tu43RiyV.png)

---

## Örnek Çıktılar (`Examples/` Dizini)

Önceden indirilmiş senkronize `.lrc` dosya örneklerini ve albüm çıktı yapılarını doğrudan bu depodaki [`Examples/`](./Examples) klasörü içinden inceleyebilirsiniz.

---

### Alternatif Diller (README)
* [English (İngilizce)](https://github.com/s0rp/CSharpSpotiLyrics/blob/main/README.md)

---

> ⚠️ **Yasal Uyarı**  
> **Bu proje eğitim amaçlı geliştirilmiştir. Spotify'ın dahili API'lerine bu şekilde erişmek Hizmet Şartları'nı ihlal edebilir. Sorumluluğu tamamen size ait olmak üzere kullanınız. Geliştiriciler, kullanımından kaynaklanabilecek herhangi bir hesap kısıtlaması veya diğer sonuçlar için hiçbir sorumluluk kabul etmez.**
> (Yine de, kendi hesabımda bir yılı aşkın süredir aktif olarak, son 5-6 aydır ise kesintisiz çalışmaktadır; şu an için herhangi bir sorun yaşanmamıştır.)

---

## Özellikler

*   **Çoklu Hedef Desteği:** Spotify Parça, Albüm veya Çalma Listesi URL'lerini ve benzersiz ID'lerini kullanarak senkronize şarkı sözü indirme.
*   **Yerel Dizin Eşleştirme:** Yerel ses dosyalarınızı tarayıp meta verilerini (ID3 etiketlerini) okur, Spotify üzerinde otomatik aratır ve `.lrc` dosyalarını doğrudan müzik dosyalarınızın yanına kaydeder.
*   **Aktif Oturum Senkronizasyonu:** Spotify hesabınızda o anda çalan aktif şarkının sözlerini anında indirebilme.
*   **İnteraktif Kütüphane Modu:** Spotify hesabınızda kayıtlı çalma listeleri veya albümler arasından terminal üzerinden interaktif seçim yaparak indirme.
*   **Standart LRC Çıktısı:** Çoğu modern medya oynatıcıyla (VLC, Poweramp, Musicolet, Foobar2000 vb.) uyumlu, standart senkronize şarkı sözü (`.lrc`) çıktısı sağlar.
*   **Sıfır Tarayıcı / Ultra Hafif Altyapı:** Playwright veya Selenium gibi hantal tarayıcı otomasyon paketlerine ihtiyaç duymaz. Tamamen C# yerleşik HTTP istemcisi ve optimize edilmiş Regex motoruyla ultra hızlı ve her platformda (Windows, Linux, macOS) sorunsuz çalışır.
*   **Esnek Yapılandırma:** Kalıcı ayarlarınız (varsayılan indirme yolu, `sp_dc` çerezi) için `config.json` dosyası kullanılır. Komut satırı seçenekleri ile bu ayarlar çalışma anında geçici olarak değiştirilebilir.
*   **Önbellek Yönetimi ve Sorun Giderme:** Bağlantı veya senkronizasyon hatası almanız durumunda TOTP anahtarlarını ve dinamik GraphQL hash dosyalarını tek bir komutla temizleme olanağı sunar.

---

## Ön Gereksinimler

*   **.NET SDK:** Kaynak kodunu derlemek ve çalıştırmak için .NET SDK 6.0 veya daha yeni bir sürüm gereklidir. [.NET SDK İndir](https://dotnet.microsoft.com/download).
*   **Spotify `sp_dc` Çerezi:** API isteklerinin doğrulanması için Spotify web oturumunuzdan alınmış geçerli bir `sp_dc` oturum çerezi gerekir.

---

## Kurulum ve Ayarlama

### 1. Depoyu klonlayın
git clone https://github.com/s0rp/CSharpSpotiLyrics

cd CSharpSpotiLyrics

### 2. CLI projesinin dizinine gidin
cd CSharpSpotiLyricsCLI

### 3. Projeyi derleyin
dotnet build -c Release

### 4. Doğrudan .NET üzerinden çalıştırın
dotnet run -- [seçenekler] <url_veya_yol>

### Veya derlenen binary dosyasını doğrudan çalıştırın:

### Windows için:
.\bin\Release\net8.0\CSharpSpotiLyricsCLI.exe [seçenekler] <url_veya_yol>

### Linux/macOS için:
./bin/Release/net8.0/CSharpSpotiLyricsCLI [seçenekler] <url_veya_yol>

Uygulamayı doğrudan .NET CLI kullanarak çalıştırabilir veya `/bin/Release/` dizini altında derlenen bağımsız binary dosyasını doğrudan terminalinizde yürütebilirsiniz.

---

## Yapılandırma

Şarkı sözlerini indirmeye başlamadan önce Spotify `sp_dc` web çerezinizi uygulamaya kaydederek kimlik doğrulaması yapmanız **gerekir**.

### 1. `sp_dc` çerezinizi nasıl alırsınız?
1. Web tarayıcınızı açın ve [open.spotify.com](https://open.spotify.com) adresine giriş yapın.
2. Tarayıcınızın Geliştirici Araçları'nı açın (genellikle `F12` veya `Sağ Tık -> İncele` ile).
3. **Uygulama (Application)** (Chrome/Edge) veya **Depolama (Storage)** (Firefox) sekmesine gidin.
4. Soldaki menüden **Çerezler (Cookies)** seçeneğini genişletin ve `https://open.spotify.com` adresini seçin.
5. Listeden `sp_dc` isimli çerezi bulun ve karşısındaki harf-rakam karışımı **Değer (Value)** alanını kopyalayın.

> **Güvenlik Uyarısı:** `sp_dc` çereziniz hesabınıza doğrudan erişim sağlar. Güvenliğini sağlayın ve asla başkalarıyla paylaşmayın.

### 2. Yapılandırma dosyasını ayarlama:
İnteraktif yapılandırma aracını çalıştırın:
```bash
dotnet run -- --config edit
```
Bu arayüz sizi şu adımlarda yönlendirecektir:
* Kopyaladığınız `sp_dc` değerini yapıştırma.
* Şarkı sözlerinin kaydedileceği varsayılan indirme dizinini belirtme.
* İndirme tercihlerinizi (örneğin dosya varsa üzerine yazılsın mı) yapılandırma.

*Not: Yapılandırma klasörü işletim sisteminize göre değişiklik gösterebilir. CLI aracı, ilk çalıştırmada veya düzenleme yaparken `config.json` dosyanızın tam konumunu size gösterecektir.*

---

## Kullanım

```bash
# dotnet run kullanarak
dotnet run -- [seçenekler] [<url_veya_yol>]

# Derlenmiş binary dosyasını doğrudan çalıştırarak
./CSharpSpotiLyrics [seçenekler] [<url_veya_yol>]
```

### Argümanlar
*   `<url_veya_yol>`: *(İsteğe bağlı)* Spotify parça/albüm/çalma listesi URL'si/ID'si veya yerel müzik klasörünüzün yolu.

### Seçenekler

| Seçenek / Komut | Açıklama |
| :--- | :--- |
| `-d`, `--directory <yol>` | Bu çalıştırma için yapılandırmadaki varsayılan indirme klasörünü geçici olarak geçersiz kılar. |
| `-f`, `--force` | Klasörde `.lrc` dosyası zaten mevcut olsa bile indirmeyi zorlar (üzerine yazar). |
| `-cl`, `--clearcache` | Senkronizasyon ve bağlantı hatalarını gidermek için yerel önbellekleri (`.SPOTIFYTOTP` ve `.SPOTIFYHASH`) temizler. |
| `-u`, `--user <öğe>` | Giriş yapmış kullanıcının kütüphanesiyle etkileşime girer. Değerler: `current`, `album`, `play`. |
| `-c`, `--config <eylem>` | Yapılandırma yardımcısını başlatır. Değerler: `edit`, `reset`, `open`. |

---

## Örnek Komutlar

### Spotify Bağlantısı veya ID ile İndirme
```bash
# Parça URL'si ile
dotnet run -- "https://open.spotify.com/track/1DwscornXpj8fmOmYVlqZt"

# Albüm ID'si ile (URI Formatı)
dotnet run -- "spotify:album:7DIlfmw6CAE1J8tp2QqgAJ"

# Çalma Listesi Bağlantısı ile (içindeki tüm şarkıları indirir)
dotnet run -- "https://open.spotify.com/playlist/1tlptlfM0epuPkqRbLHvdj"
```

### Yerel Klasör Taraması ve Eşleştirme
```bash
# Klasördeki müzik dosyalarının meta verilerini tarayıp otomatik olarak sözleri indirir
dotnet run -- "/home/kullanici/Muzik/MuzikKlasorüm"
```

### Aktif Oturum ve Kütüphane Etkileşimi
```bash
# Spotify'da o anda aktif olarak çalan şarkınızın sözlerini indirir
dotnet run -- --user current

# Kütüphanenizde kayıtlı çalma listelerini listeleyip interaktif seçerek indirir
dotnet run -- --user play

# Kütüphanenizde kayıtlı albümleri listeleyip interaktif seçerek indirir
dotnet run -- --user album
```

### Yapılandırma ve Sorun Giderme
```bash
# Şarkı sözleri klasörde mevcut olsa bile indirmeyi zorla
dotnet run -- --force "https://open.spotify.com/track/1DwscornXpj8fmOmYVlqZt"

# Önbellekteki geçici anahtarları sıfırlayarak olası "Bad Request" hatalarını temizle
dotnet run -- --clearcache "https://open.spotify.com/track/1DwscornXpj8fmOmYVlqZt"

# config.json dosyasının bulunduğu klasörü Dosya Gezgini'nde aç
dotnet run -- --config open
```

---

## Sorun Giderme

Eğer istemci başlatılırken kimlik doğrulama veya bağlantı hataları (örneğin `400 Bad Request`) alırsanız:
1. `sp_dc` çerezinizin süresinin dolmadığından emin olun. Tarayıcınızdan Spotify Web Oynatıcısına girdiğinizde hala oturumunuzun açık olduğunu doğrulayarak bunu test edebilirsiniz.
2. Uygulamayı `-cl` / `--clearcache` seçeneğiyle çalıştırın. Bu işlem yerel geçici token'ları ve dinamik hash dosyalarını temizler; uygulamanın Spotify ile bağlantıyı en baştan kurarak dinamik GraphQL hash'lerini otomatik olarak yenilemesini sağlar.

---

## Katkıda Bulunanlar

*   **Geliştirme & C# Çekirdek Mimarisi:** s0rp
*   **Workflow Yeniden Yazımı & Kod Düzenlemeleri:** Dixiz 3A (MoE Project Neural Supervisor)

--

## Marka Sorumluluk Reddi (Trademark Disclaimer)

Bu proje tamamen bağımsız, açık kaynaklı bir topluluk çalışmasıdır. **Spotify AB**, Spotify markası, onun iştirakleri, platformda yer alan sanatçılar veya müzik şirketleriyle hiçbir resmi bağı, ortaklığı, sponsorluğu veya alakası bulunmamaktadır. "Spotify" ismi, logosu ve ilgili tüm tescilli markalar Spotify AB firmasına aittir.