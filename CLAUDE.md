# SolidAssist

C# WinForms uygulama, SOLIDWORKS API ile parametrik mil + kama kanalı oluşturma. .NET Framework 4.7.2.

## Mimari

| Dosya | Sorumluluk |
|---|---|
| `MainForm.cs` | Ana UI: çap/uzunluk input, Mil Oluştur + Kama Kanalı butonları |
| `ShaftBuilder.cs` | SW API ile silindir extrude, plane/sketch yardımcıları |
| `KeywayForm.cs` | Kama kanalı UI: standart seçimi, kenar mesafesi |
| `KeywayBuilder.cs` | Ref plane offset + slot sketch + FeatureCut4 cut-extrude |
| `KeywayStandard.cs` | Standart kama boyutları tablosu |

**Pattern:** UI Form → static Builder class → SW API çağrısı.

## Birim sistemi

- UI: mm
- SW API: m
- Çevrim **builder içinde** yapılır, UI'da değil.

## SOLIDWORKS Türkçe kurulum

Hedef SW Türkçe arayüzlü. Plane/feature adları lokalize ("Ön Düzlem", "Üst Düzlem").

- Plane adlarını `ShaftBuilder.GetPlaneName(doc, idx)` ile çöz, hardcode "Top Plane" yazma.
- Yeni `SelectByID2` çağrısında lokalize ad düşün.
- Hata mesajları Türkçe yaz.

## Kama kanalı koordinat sistemi

`KeywayBuilder` referans düzlemi Top Plane'e paralel, +radius offset (silindirin tepesine teğet).

Sketch koordinatları: sketch +X = world X, sketch +Y = world **-Z** (negatif). Z=0 ucuna yerleştirmek için y koordinatları **negatif** olmalı (`KeywayBuilder.cs:60-63`).

## Geliştirme ortamı

İki PC arası bölünmüş geliştirme:
- **Şirket PC** — SOLIDWORKS kurulu, ana geliştirme
- **Kişisel PC** — SW yok, sadece derleme/edit

Senkron için GitHub remote: `MetinKagan/SolidAssist` (origin/main).

Kişisel PC'de SW API çağrısı çalıştırılamaz — derleme ile doğrula, runtime test kullanıcıya bırak.

## Releases

- `releases/v1_mil/` — sadece mil
- `releases/v2_kama_test/` — mil + kama test build

## Sketch API gotchas

**CreateLine/CreateArc otomatik birleşmez.** Aynı koordinatla iki segment çizsen bile endpoint'ler yapışık değildir → contour açık → cut "ince unsur" yorumlanır.

Her köşede explicit coincident:
```csharp
SketchLine line = (SketchLine)swSketchMgr.CreateLine(...);
SketchArc arc   = (SketchArc)swSketchMgr.CreateArc(...);
doc.ClearSelection2(true);
((SketchPoint)line.GetStartPoint2()).Select4(false, null);
((SketchPoint)arc.GetStartPoint2()).Select4(true, null);
doc.SketchAddConstraints("sgCOINCIDENT");
```

**`SketchSegment`** üzerinde GetStartPoint2 yok. Cast `SketchLine` veya `SketchArc`.

**`FeatureCut4`** bu kurulumda 27 arg. `UseFeatScope=true, UseAutoSelect=true` (pos 19-20) zorunlu.

**`CreateSketchSlot`** enum: `swSketchSlotCreationType_line`, `swSketchSlotLengthType_CenterCenter`. 14. arg `bool AddDimension=true` → otomatik ölçü.

**Sketch reference** — `swSketchMgr.ActiveSketch` ile çıkmadan yakala, isim aramaktan kaçın.

## Convention

- Yeni feature: yeni Form + yeni Builder static class
- SW API'da her zaman `Marshal.GetActiveObject("SldWorks.Application")` ile aktif instance al, null check
- `swDoc.ClearSelection2(true)` her API çağrısı öncesi
- `Application.DoEvents()` uzun süren UI bloklarında durum güncellemesi için
