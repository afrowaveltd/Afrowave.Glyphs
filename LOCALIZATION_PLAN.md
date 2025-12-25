# Implementace lokalizace - Akční plán

## 🎯 **Cíl:**
Přidat Czech a English lokalizaci do Avalonia editoru

## 📋 **Kroky:**

### **1. Vytvořit translation soubory:**

**App/Localization/Translations/en.json:**
```json
{
  "App.Title": "Afrowave Glyph Editor",
  "Menu.Workspace": "Workspace",
  "Menu.NewStyle": "New Style",
  "Menu.Import": "Import Font",
  "Menu.Symbols": "Create Symbols",
  "Menu.Range": "Unicode Range",
  "Menu.Browse": "Browse Glyphs",
  "Editor.Pack": "Pack:",
  "Editor.Style": "Style:",
  "Editor.Size": "Size:",
  "Editor.Width": "W:",
  "Editor.Height": "H:",
  "Editor.SelectGlyph": "Select glyph:",
  "Editor.Save": "Save",
  "Editor.Revert": "Revert",
  "Editor.Clear": "Clear",
  "Editor.Clone": "Clone",
  "Editor.Edit": "Edit",
  "Preview.Title": "Preview",
  "Preview.EnterText": "Enter text to preview..."
}
```

**App/Localization/Translations/cs.json:**
```json
{
  "App.Title": "Afrowave Editor Glyphů",
  "Menu.Workspace": "Pracovní prostor",
  "Menu.NewStyle": "Nový styl",
  "Menu.Import": "Importovat font",
  "Menu.Symbols": "Vytvořit symboly",
  "Menu.Range": "Unicode rozsah",
  "Menu.Browse": "Procházet glyphy",
  "Editor.Pack": "Balíček:",
  "Editor.Style": "Styl:",
  "Editor.Size": "Velikost:",
  "Editor.Width": "Š:",
  "Editor.Height": "V:",
  "Editor.SelectGlyph": "Vybrat glyph:",
  "Editor.Save": "Uložit",
  "Editor.Revert": "Vrátit",
  "Editor.Clear": "Vymazat",
  "Editor.Clone": "Klonovat",
  "Editor.Edit": "Editovat",
  "Preview.Title": "Náhled",
  "Preview.EnterText": "Zadej text pro náhled..."
}
```

### **2. Rozšířit LocalizationService:**

```csharp
public class LocalizationService
{
    private Dictionary<string, string> _translations;
    private string _currentLanguage = "en";
    
    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            _currentLanguage = value;
            LoadTranslations(value);
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    
    public event EventHandler? LanguageChanged;
    
    public string Translate(string key, string? fallback = null)
    {
        if (_translations.TryGetValue(key, out var value))
            return value;
        return fallback ?? key;
    }
    
    public string this[string key] => Translate(key);
}
```

### **3. Přidat do UI:**

**MainWindow.axaml:**
```xml
<StackPanel Orientation="Horizontal">
  <Button Content="{Binding Localization[Menu.Workspace]}" />
  <Button Content="{Binding Localization[Menu.NewStyle]}" />
  <Button Content="{Binding Localization[Menu.Import]}" />
</StackPanel>
```

### **4. Language selector:**

**Settings dialog:**
```xml
<ComboBox SelectedItem="{Binding CurrentLanguage}">
  <ComboBoxItem Content="English" Tag="en" />
  <ComboBoxItem Content="Čeština" Tag="cs" />
</ComboBox>
```

---

## ✅ **Výsledek:**

- ✅ Czech/English přepínání
- ✅ Všechny texty přeložené
- ✅ Persistence v settings
- ✅ Runtime změna jazyka

**Odhadovaný čas:** 2-4 hodiny
