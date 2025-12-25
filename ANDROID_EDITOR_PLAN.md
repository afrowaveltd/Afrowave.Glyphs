# Android Glyph Editor - Plán

## 🎯 **Vize:**
Mobilní verze Glyph Editoru pro vytváření glyphů na cestách

## 📱 **Avalonia.Mobile možnosti:**

### **1. Sdílený kód:**
```
Afrowave.Glyphs/
  ├── Core/                    ✅ Sdíleno
  ├── Storage.Abstractions/    ✅ Sdíleno
  ├── Storage.FileSystem/      ✅ Sdíleno
  ├── Tools/                   ✅ Sdíleno (s úpravami)
  ├── Editor.Avalonia/         ❌ Desktop only
  └── Editor.Mobile/           🆕 Nový projekt!
```

### **2. Mobile-specific features:**

#### **Touch-friendly UI:**
```xml
<!-- Větší buttony pro touch -->
<Button Width="80" Height="80" />

<!-- Gestures -->
- Pinch to zoom
- Swipe to navigate
- Long press for options
```

#### **Pixel Editor pro touch:**
```
┌─────────────────────────────┐
│  ████████                   │
│  ██    ██                   │
│  ██    ██                   │
│  ████████                   │
│                             │
│  [Tap pixel to toggle]      │
│                             │
│  [Undo] [Redo] [Clear]      │
│  [Save] [Cancel]            │
└─────────────────────────────┘
```

#### **Font import z Android systému:**
```csharp
// Použít Android system fonts
var fontPath = "/system/fonts/Roboto-Regular.ttf";
await ImportFont(fontPath);
```

#### **Cloud sync:**
```csharp
// Export do Google Drive / Dropbox
await ExportToCloud(workspace, "MyGlyphs.zip");

// Import z cloud
await ImportFromCloud("MyGlyphs.zip");
```

---

## 🏗️ **Architektura:**

### **Projekt struktura:**

```
Editor.Mobile/
  ├── Android/              # Android-specific
  │   ├── MainActivity.cs
  │   └── AndroidManifest.xml
  ├── iOS/                  # iOS-specific (budoucnost)
  ├── Views/
  │   ├── MainView.axaml    # Touch-optimized
  │   ├── EditorView.axaml  # Touch pixel grid
  │   └── SettingsView.axaml
  └── Services/
      ├── CloudStorageService.cs
      └── AndroidFontService.cs
```

### **Touch Glyph Editor:**

```csharp
public class TouchGlyphEditorView : UserControl
{
    private void OnPixelTapped(int x, int y)
    {
        var pixel = Bitmap.GetPixel(x, y);
        Bitmap.SetPixel(x, y, !pixel);
        InvalidateVisual();
    }
    
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var point = e.GetPosition(this);
        var x = (int)(point.X / PixelSize);
        var y = (int)(point.Y / PixelSize);
        OnPixelTapped(x, y);
    }
}
```

---

## 📋 **Workflow:**

### **1. Vytvoření na mobilu:**
```
1. Otevři Editor.Mobile
2. Vyber pack/style
3. Import font z Android systému
   nebo
   Vytvoř glyphs ručně (tap to draw)
4. Save workspace
```

### **2. Sync do PC:**
```
Option A: Cloud
- Export to Google Drive
- Download na PC
- Import do Desktop Avalonia

Option B: USB
- Export to phone storage
- Přenos přes USB
- Import do Desktop Avalonia

Option C: Network
- WebDAV / SMB share
- Direct folder access
```

---

## ✅ **Benefits:**

1. ✅ **Vytváření glyphů kdekoli** - v MHD, na cestách
2. ✅ **Použití Android fontů** - systémové fonty dostupné
3. ✅ **Touch-friendly** - optimalizováno pro prsty
4. ✅ **Cloud sync** - automatické zálohování
5. ✅ **Sdílený kód** - 80% kódu reused z Desktop

---

## 🎯 **Implementace fáze:**

### **Phase 1: Proof of Concept (1-2 týdny)**
- ✅ Vytvořit Editor.Mobile projekt
- ✅ Základní UI (pack/style selector)
- ✅ Touch pixel editor
- ✅ Save/Load workspace

### **Phase 2: Font Import (1 týden)**
- ✅ List Android system fonts
- ✅ Import selected font
- ✅ Rasterize glyphs

### **Phase 3: Cloud Sync (1 týden)**
- ✅ Export to ZIP
- ✅ Google Drive integration
- ✅ Import from ZIP

### **Phase 4: Polish (1 týden)**
- ✅ Lokalizace
- ✅ Themes
- ✅ Settings
- ✅ Help/Tutorial

---

## 📊 **Priorita:**

**HIGH** - Unikátní value proposition!
- Jediný mobilní glyph editor
- Využití Android fontů
- On-the-go creation

**Odhadovaný čas:** 4-6 týdnů development

---

## 💡 **Marketing angle:**

> "Create pixel-perfect glyphs anywhere, anytime!
> Import Android system fonts, design on your phone,
> sync to desktop for final export."

**Target audience:**
- Pixel artists
- Font designers
- Terminal enthusiasts
- Retro game developers

---

**This could be a KILLER feature!** 🚀📱
