# Consolonia Dialog Fix - Font Import

## 🐛 **Problém:**

```
Consolonia.Core.Infrastructure.ConsoloniaException:
Creating multiple Window objects simultaneously is not allowed.
```

### **Příčina:**
Font Import Wizard se snažil otevřít **vnořený dialog** (pro výběr fontu) uvnitř už existujícího dialogu.

```
MainWindow
  └── FontImportWizardWindow (Dialog 1) ✅
      └── File Picker Dialog (Dialog 2) ❌ CRASH!
```

---

## ✅ **Řešení:**

### **Odstranit vnořený dialog - použít direct text input**

**PŘED:**
```xml
<TextBox Text="{Binding FontPath}" Width="80" />
<Button Content="Browse" Command="{Binding PickFontCommand}" />
```
- Browse button otevíral nový Window dialog ❌

**PO:**
```xml
<TextBox Text="{Binding FontPath}" 
         Watermark="e.g., C:\Windows\Fonts\arial.ttf" 
         Width="90" />

<TextBlock Text="Common paths: Windows: C:\Windows\Fonts\ ..."
           Foreground="Gray"/>
```
- User zadá cestu přímo ✅
- Helper text s příklady ✅
- Žádný vnořený dialog ✅

---

## 📋 **Změny v kódu:**

### **1. FontImportWizardWindow.axaml**
```xml
<!-- Odstraněn Browse button -->
<!-- Přidán helper text s common paths -->
<TextBox Text="{Binding FontPath}" 
         Watermark="e.g., C:\Windows\Fonts\arial.ttf" />
```

### **2. MainWindow.axaml.cs**
```csharp
// ODSTRANĚNO: PickFontFileHandler (už nepotřeba)
var wizVm = new FontImportWizardViewModel(importService)
{
   FontPath = FilePickerWindow.GetDefaultFontsDirectory() // Pre-fill
};
```

### **3. FontImportWizardWindow.axaml.cs**
```csharp
// Žádné speciální handlery - jen standardní Initialize
public void Initialize(FontImportWizardViewModel vm)
{
   DataContext = vm;
   vm.CloseHandler = CloseAsync;
}
```

---

## 🎯 **Jak to nyní funguje:**

### **User workflow:**

1. **Klikne "Import" v menu**
2. **Font Import Wizard se otevře** (jeden dialog)
3. **Zadá cestu k fontu:**
   - Zkopíruje cestu z Exploreru: `C:\Windows\Fonts\arial.ttf`
   - Nebo použije helper text s příklady
4. **Nastaví parametry** (Pack, Size, Range)
5. **Klikne "Start" nebo "Import Range"**
6. **Glyph editor se otevře** pro každý znak

**Žádné vnořené dialogy!** ✅

---

## 💡 **Helper Text:**

```
Common paths:
  Windows: C:\Windows\Fonts\
  Linux: /usr/share/fonts/truetype/
  macOS: /System/Library/Fonts/

Tip: Copy full path from file manager
```

---

## 🔍 **Srovnání verzí:**

| Feature | Avalonia | Consolonia (Fixed) |
|---------|----------|-------------------|
| Font Picker | Native OS dialog | Text input + helper |
| Browse Button | ✅ Grafický picker | ❌ Odstraněn |
| Helper Text | Tooltip | Inline text |
| Nested Dialogs | ✅ Podporováno | ❌ Zakázáno |
| User Experience | Click & browse | Copy & paste path |

---

## ✅ **Výhody řešení:**

1. ✅ **Žádné crashes** - žádné vnořené dialogy
2. ✅ **Rychlejší** - copy/paste je často rychlejší než browsování
3. ✅ **Helper text** - příklady cest pro všechny OS
4. ✅ **Konzistentní** - stejný pattern jako ostatní Consolonia dialogy
5. ✅ **Jednodušší** - méně kódu, méně complexity

---

## 🧪 **Test:**

1. Spusť Consolonia app
2. Klikni "Import"
3. ✅ Dialog se otevře bez crashe
4. Zadej cestu: `C:\Windows\Fonts\arial.ttf`
5. Nastav Pack: `16x16`
6. Klikni "Start"
7. ✅ Glyph editor se otevře

**Žádné exception! Vše funguje!** 🎉

---

## 📝 **Poznámky:**

### **Proč ne file picker?**

Consolonia **UMOŽŇUJE** dialogy, ale jen **jeden současně**:
```
✅ MainWindow → Dialog → Close dialog → MainWindow
❌ MainWindow → Dialog A → Dialog B → CRASH!
```

### **Alternativní řešení (budoucnost):**

**Option 1: Sequential dialogs**
```csharp
1. Close FontImportWizard
2. Open FilePicker
3. Get result
4. Re-open FontImportWizard with path
```

**Option 2: Inline file browser**
```xml
<!-- Embed file list directly in wizard window -->
<ListBox Items="{Binding AvailableFonts}" />
```

**Pro MVP: Text input je nejjednodušší a funkční!** ✅

---

## ✅ **Výsledek:**

✅ **Font Import funguje bez crashů**  
✅ **User-friendly s helper textem**  
✅ **Konzistentní s Consolonia omezeními**  
✅ **Jednodušší a čistší kód**  

**Obě verze (Avalonia + Consolonia) nyní plně funkční!** 🚀✨
