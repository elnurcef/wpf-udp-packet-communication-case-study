# WPF UDP Paket Haberleşme Simülatörü

Bu proje, WPF tabanlı iki ayrı masaüstü arayüzün UDP üzerinden özel bir binary paket protokolü ile haberleşmesini sağlayan teknik bir vaka çalışmasıdır.

Projede bir adet kullanıcı/operator arayüzü, bir adet simülasyon arayüzü ve ortak haberleşme kütüphanesi bulunmaktadır.

## İçindekiler

* [Proje Özeti](#proje-özeti)
* [Teknolojiler](#teknolojiler)
* [Solution Yapısı](#solution-yapısı)
* [Uygulamalar](#uygulamalar)
* [Paket Formatı](#paket-formatı)
* [UDP Portları](#udp-portları)
* [CRC Algoritması](#crc-algoritması)
* [Projeyi Derleme](#projeyi-derleme)
* [Uygulamaları Çalıştırma](#uygulamaları-çalıştırma)
* [Language Selection](#language-selection)
* [Önerilen Çalıştırma Sırası](#önerilen-çalıştırma-sırası)
* [I. Etap Özellikleri](#i-etap-özellikleri)
* [II. Etap Otomasyon Testleri](#ii-etap-otomasyon-testleri)
* [Loglama](#loglama)
* [Log Oynatma](#log-oynatma)
* [MATLAB Dönüşümü](#matlab-dönüşümü)
* [Test Raporları](#test-raporları)
* [Manuel Test Akışı](#manuel-test-akışı)
* [Önemli Notlar](#önemli-notlar)

## Proje Özeti

Bu proje iki ayrı WPF uygulamasından oluşur:

1. **User Interface**

   * Kullanıcı/operator arayüzüdür.
   * Simülasyon arayüzünden gelen haberleşme paketlerini dinler.
   * Paket verilerini ekranda gösterir.
   * Komut ve ayar paketleri gönderir.
   * Geri besleme paketlerini alır.
   * Haberleşme verilerini loglar.
   * Kaydedilmiş logları oynatır.
   * Log dosyalarını MATLAB `.mat` dosyasına dönüştürür.

2. **Simulation Interface**

   * Aviyonik/gömülü sistem birimini simüle eden WPF arayüzüdür.
   * 5 Hz frekansında haberleşme paketleri gönderir.
   * Kullanıcı arayüzünden gelen komut ve ayar paketlerini alır.
   * Gelen komut ve ayarlara karşılık geri besleme paketi gönderir.

3. **Shared Library**

   * Paket modellerini, enumları, CRC hesaplamasını, paket oluşturma/doğrulama işlemlerini, UDP haberleşme servisini, loglama ve MATLAB dönüşüm servislerini içerir.

## Teknolojiler

* C#
* .NET 10
* WPF
* MVVM tarzı yapı
* UDP haberleşme
* Custom binary packet protocol
* CRC-8/ATM
* Little Endian serialization
* CSV loglama
* MATLAB `.mat` dönüşümü
* UI Automation test runner
* PDF test raporu üretimi

## Solution Yapısı

```text
wpf-udp-packet-communication-case-study/
│
├── Baykar.UserInterface/
│   └── Kullanıcı/operator WPF arayüzü
│
├── Baykar.SimulationInterface/
│   └── Simüle edilmiş aviyonik/gömülü birim WPF arayüzü
│
├── Baykar.Shared/
│   └── Ortak paket, haberleşme, CRC, loglama ve MATLAB servisleri
│
├── Baykar.UiAutomationTests/
│   └── II. etap otomatik UI test runner projesi
│
├── BaykarCaseStudy.sln
├── PROJECT_RULES.md
└── README.md
```

## Uygulamalar

### Baykar.UserInterface

Kullanıcı tarafındaki ana arayüzdür.

Başlıca görevleri:

* UDP listener başlatma/durdurma
* CommunicationPacket1 ve CommunicationPacket2 alma
* Gelen paketleri doğrulama
* Paket sayaçlarını gösterme
* CommandPacket gönderme
* SettingPacket gönderme
* FeedbackPacket alma
* CSV log oluşturma
* Log oynatma
* MATLAB dönüşümü yapma

### Baykar.SimulationInterface

Simüle edilmiş aviyonik/gömülü birim arayüzüdür.

Başlıca görevleri:

* UDP listener başlatma/durdurma
* CommunicationPacket1 ve CommunicationPacket2 paketlerini 5 Hz ile gönderme
* CommandPacket alma
* SettingPacket alma
* FeedbackPacket gönderme
* Son gelen komut ve ayar bilgisini ekranda gösterme

### Baykar.Shared

Ortak kütüphanedir.

İçerdiği başlıca yapılar:

* Packet enumları
* Payload struct modelleri
* Little Endian serialization/deserialization
* CRC-8 hesaplayıcı
* PacketBuilder
* PacketValidator
* PacketCaptureStateMachine
* UDP communication service
* CSV log service
* MATLAB conversion service

### Baykar.UiAutomationTests

II. etap için otomatik UI test runner projesidir.

Başlıca görevleri:

* JSON test script okumak
* WPF UI elementlerini AutomationId ile bulmak
* Butonları otomatik tetiklemek
* TextBox değerlerini otomatik girmek
* Haberleşme ve geri besleme kontrolleri yapmak
* Test sonuçlarını konsola yazdırmak
* PDF test raporu oluşturmak

## Paket Formatı

Tüm paketler aşağıdaki binary formatı kullanır:

```text
Sync1 | Sync2 | PacketId | PayloadLength | Payload | CRC
```

| Alan          | Açıklama                   |
| ------------- | -------------------------- |
| Sync1         | Birinci senkron byte       |
| Sync2         | İkinci senkron byte        |
| PacketId      | Paket türünü belirten byte |
| PayloadLength | Payload uzunluğu           |
| Payload       | Pakete özel veri alanı     |
| CRC           | CRC-8 kontrol byte'ı       |

Senkron byte değerleri:

```text
Sync1 = 169
Sync2 = 233
```

## Paket Türleri

| Paket Türü           | ID | Yön                                 |
| -------------------- | -: | ----------------------------------- |
| CommunicationPacket1 |  3 | SimulationInterface → UserInterface |
| CommunicationPacket2 |  4 | SimulationInterface → UserInterface |
| SettingPacket        |  5 | UserInterface → SimulationInterface |
| CommandPacket        |  6 | UserInterface → SimulationInterface |
| FeedbackPacket       |  7 | SimulationInterface → UserInterface |

## UDP Portları

Uygulamalar localhost üzerinden haberleşir.

| Uygulama            | Local Port | Remote Target  |
| ------------------- | ---------: | -------------- |
| SimulationInterface |       5000 | 127.0.0.1:5001 |
| UserInterface       |       5001 | 127.0.0.1:5000 |

## CRC Algoritması

Projede kullanılan CRC algoritması:

```text
CRC-8/ATM
Polynomial: 0x07
Initial value: 0x00
Final XOR: 0x00
```

CRC hesabına dahil edilen alanlar:

```text
Sync1 + Sync2 + PacketId + PayloadLength + Payload
```

CRC byte'ı hesaplamaya dahil edilmez. Hesaplanan CRC paketin sonuna eklenir.

## Projeyi Derleme

Solution root dizininde aşağıdaki komut çalıştırılır:

```bash
dotnet build
```

## Uygulamaları Çalıştırma

Aynı anda birden fazla terminal kullanılması önerilir.

### Terminal 1 — User Interface

```bash
dotnet run --project .\Baykar.UserInterface\Baykar.UserInterface.csproj
```

### Terminal 2 — Simulation Interface

```bash
dotnet run --project .\Baykar.SimulationInterface\Baykar.SimulationInterface.csproj
```

### Terminal 3 — UI Automation Tests

```bash
dotnet run --project .\Baykar.UiAutomationTests\Baykar.UiAutomationTests.csproj
```

## Dil seçimi

Her iki wpf Ingilizce ve Türkceyi destekliyor. İngilizce default dildir. WPF-lerde sağ üstten dil seçimi yapa bilirsiniz `English` and `Türkçe`.

Otomasyon testlerinde de Türkce ve İngilizce çıktılar almak mümkün:

```bash
dotnet run --project .\Baykar.UiAutomationTests\Baykar.UiAutomationTests.csproj -- --lang tr
dotnet run --project .\Baykar.UiAutomationTests\Baykar.UiAutomationTests.csproj -- --lang en
```

## Önerilen Çalıştırma Sırası

I. etap manuel test için:

1. `Baykar.UserInterface` çalıştırılır.
2. UserInterface içinde **Start Listener** butonuna basılır.
3. `Baykar.SimulationInterface` çalıştırılır.
4. SimulationInterface içinde **Start Simulation** butonuna basılır.
5. UserInterface tarafında CommunicationPacket1 ve CommunicationPacket2 paketlerinin geldiği kontrol edilir.
6. UserInterface üzerinden Command ve Setting paketleri gönderilir.
7. SimulationInterface tarafında komut/ayarın alındığı kontrol edilir.
8. UserInterface tarafında FeedbackPacket geldiği kontrol edilir.

II. etap otomasyon testi için:

1. `Baykar.UserInterface` çalıştırılır.
2. `Baykar.SimulationInterface` çalıştırılır.
3. SimulationInterface içinde **Start Simulation** butonuna basılır.
4. `Baykar.UiAutomationTests` çalıştırılır.
5. Otomasyon test runner UserInterface uygulamasına attach olur.
6. Test adımları JSON script üzerinden çalıştırılır.
7. Sonuçlar konsola yazılır.
8. PDF test raporu oluşturulur.

Not: Otomasyon test runner, UserInterface üzerinde Start Listener butonuna kendisi basar. Ancak haberleşme ve feedback testlerinin geçmesi için SimulationInterface açık olmalı ve Start Simulation başlatılmış olmalıdır.

## I. Etap Özellikleri

I. etap kapsamında geliştirilen başlıca özellikler:

* İki ayrı WPF arayüz
* UDP ile haberleşme
* 5 Hz veri gönderimi
* Binary paket oluşturma
* Binary paket çözme
* Little Endian veri dönüşümü
* CRC-8 doğrulama
* Byte-by-byte packet capture state machine
* Sağlıklı paket sayacı
* Hatalı paket sayacı
* Komut paketi gönderme
* Ayar paketi gönderme
* Geri besleme paketi alma/gönderme
* CSV loglama
* Log oynatma
* MATLAB `.mat` dönüşümü

## II. Etap Otomasyon Testleri

II. etap kapsamında otomatik UI test runner geliştirilmiştir.

Test runner, WPF arayüzdeki elementleri `AutomationId` değerleriyle bulur ve test scriptteki adımları çalıştırır.

Desteklenen temel test aksiyonları:

* `click`
* `setTextAndClick`
* `waitForText`
* `waitForTextContains`
* `waitForTextNotEmpty`
* `waitForNumericGreaterThan`
* `clickAndWaitForText`

Test script dosyası:

```text
Baykar.UserInterface/Release/Test/default-test-script.json
```

Test runner çalıştırma komutu:

```bash
dotnet run --project .\Baykar.UiAutomationTests\Baykar.UiAutomationTests.csproj
```

Başarılı örnek konsol çıktısı:

```text
Test: Default User Interface Automation Test
[PASS] Start user interface listener
[PASS] Wait for Communication Packet 1 count
[PASS] Wait for Communication Packet 2 count
[PASS] Send Command 1 and verify feedback
[PASS] Send Setting 1 and verify feedback
[PASS] Send Command 1
[PASS] Send Setting 1
[PASS] Start logging
[PASS] Stop logging

Final Result: PASSED
PDF Report: ...\Release\TestReports\test-report-YYYYMMDD-HHMMSS.pdf
```

## Loglama

UserInterface, gelen CommunicationPacket1 ve CommunicationPacket2 verilerini CSV formatında loglayabilir.

Log dosyaları aşağıdaki klasöre kaydedilir:

```text
Release/Log Kayıtları
```

Log dosyaları çalışma zamanında oluşturulur ve repository'ye dahil edilmemelidir.

## Log Oynatma

Kaydedilen CSV log dosyaları UserInterface üzerinden tekrar oynatılabilir.

Log oynatma sırasında aktif UDP haberleşmesi gerekmez. CSV dosyasındaki kayıtlar sırayla okunur ve UI üzerinde tekrar gösterilir.

## MATLAB Dönüşümü

Kaydedilen CSV log dosyaları MATLAB `.mat` formatına dönüştürülebilir.

MATLAB çıktı dosyaları aşağıdaki klasöre kaydedilir:

```text
Release/MATLAB Dönüsümleri
```

Oluşturulan `.mat` dosyası CommunicationPacket1 ve CommunicationPacket2 verilerini ayrı değişkenler halinde içerir.

## Test Raporları

UI automation testleri tamamlandıktan sonra PDF test raporu oluşturulur.

Rapor klasörü:

```text
Release/TestReports
```

PDF rapor içeriği:

* Test adı
* Test başlangıç zamanı
* Test bitiş zamanı
* Test süresi
* Her test adımı
* Her test adımının başarılı/başarısız sonucu
* Test adımı mesajı
* Nihai test sonucu

## Manuel Test Akışı

Teslim öncesi önerilen manuel test akışı:

1. Solution build edilir.
2. UserInterface çalıştırılır.
3. SimulationInterface çalıştırılır.
4. UserInterface üzerinde Start Listener basılır.
5. SimulationInterface üzerinde Start Simulation basılır.
6. CommunicationPacket1 ve CommunicationPacket2 alındığı doğrulanır.
7. Valid packet count değerinin arttığı kontrol edilir.
8. Command 1 gönderilir.
9. SimulationInterface üzerinde komutun alındığı kontrol edilir.
10. UserInterface üzerinde feedback alındığı kontrol edilir.
11. Setting 1 gönderilir.
12. SimulationInterface üzerinde ayar ve değerinin alındığı kontrol edilir.
13. UserInterface üzerinde feedback alındığı kontrol edilir.
14. Logging başlatılır.
15. Birkaç saniye sonra logging durdurulur.
16. CSV log dosyasının oluştuğu kontrol edilir.
17. Log playback çalıştırılır.
18. MATLAB dönüşümü yapılır.
19. `.mat` dosyasının oluştuğu kontrol edilir.
20. Simulation ve listener durdurulur.
21. Uygulamaların hata vermeden kapandığı kontrol edilir.
22. UI automation test runner çalıştırılır.
23. Test sonucunun `PASSED` olduğu kontrol edilir.
24. PDF test raporunun oluştuğu kontrol edilir.

## Önemli Notlar

* Paket serialization işlemleri Little Endian byte order ile yapılır.
* Paket yakalama byte-by-byte state machine ile yapılır.
* UserInterface içinde Timer kullanılmaz.
* Periyodik gönderim async loop ve `Task.Delay` ile yapılır.
* Background işlemler `Task`, `CancellationToken` ve event yapıları ile yönetilir.
* Background thread üzerinden UI update yapılırken Dispatcher kullanılır.
* Runtime çıktıları repository'ye dahil edilmemelidir.
* PDF vaka dokümanı repository'ye eklenmemelidir.

## Bilinen Veri Notu

CommunicationPacket1 içinde `Data5` alanı `INT16` olarak tanımlıdır.

Örnek dokümandaki `Data5` değeri `72635` olarak görünmektedir. Bu değer `Int16` aralığının dışındadır. Bu nedenle implementasyonda örnek değer olarak `0` kullanılmıştır.

## Git Ignore Önerisi

Aşağıdaki runtime ve build çıktıları repository'ye commit edilmemelidir:

```gitignore
bin/
obj/
.vs/
.vscode/
*.user
*.suo

**/Release/Log Kayıtları/
**/Release/MATLAB Dönüsümleri/
**/Release/TestReports/

*.log
*.mat
*.pdf
```
