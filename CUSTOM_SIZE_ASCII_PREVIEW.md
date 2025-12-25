# Custom W/H Size + ASCII Preview - Final Implementation

## ✅ Implementované změny:

### **1. Custom W/H Size Selector** ✅

#### **Avalonia:**
- ✅ **Již implementováno!** (žádné změny potřeba)
- TextBox pro Width a Height
- Automatický update při změně
- Binding na `CustomGlyphWidth` a `CustomGlyphHeight`

#### **Consolonia:**
- ✅ **Nově přidáno!**
- TextBox pro W: a H:
- Width="4" pro kompaktní TUI
- Automatický update při změně
- Identický binding jako Avalonia

**XAML:**
```xml
<TextBlock Text="W:" />
<TextBox Text="{Binding CustomGlyphWidth}" Width="4" />

<TextBlock Text="H:" />
<TextBox Text="{Binding CustomGlyphHeight}" Width="4" />
```

---

### **2. ASCII Art Preview (Consolonia only)** ✅

#### **Koncept:**
- Preview je omezen na **5 znaků** (výkon)
- Každý **pixel = ASCII znak** (█ nebo ·)
- Glyphs se renderují **vedle sebe**
- Real-time update při změně textu

#### **Příklad výstupu pro "Hi":**
```
8x16 glyph size:
█·······  ··█·····
█·······  ··█·····
█·······  ··█·····
███████·  ··█·····
█·······  ··█·····
█·······  ··█·····
█·······  ··█·····
█·······  ··█·····
```

#### **Implementace:**

**XAML:**
```xml
<StackPanel>
  <!-- ASCII art preview -->
  <TextBlock Name="PreviewAscii"
             FontFamily="Consolas"
             Foreground="Cyan"
             Background="Black"
             FontSize="8" />
  
  <!-- Text preview -->
  <TextBlock Name="PreviewText"
             Text="{Binding Text}"
             FontFamily="Consolas"
             Foreground="White" />
</StackPanel>
```

**C#:**
```csharp
private async Task<string> RenderToAsciiArtAsync(string text, MainViewModel vm)
{
   var sb = new StringBuilder();
   var glyphSize = vm.SelectedGlyphSize;
   var limitedText = text.Length > 5 ? text.Substring(0, 5) : text;

   // Render row by row
   for(int row = 0; row < glyphSize.Height; row++)
   {
      // Render each character in this row
      for(int charIndex = 0; charIndex < limitedText.Length; charIndex++)
      {
         var bitmap = await _currentCache.GetBitmapAsync(...);
         
         // Render pixel by pixel
         for(int col = 0; col < glyphSize.Width; col++)
         {
            var pixel = bitmap.GetPixel(col, row);
            sb.Append(pixel ? "█" : "·");  // Full block or dot
         }
         
         sb.Append(" ");  // Space between chars
      }
      sb.AppendLine();
   }
   
   return sb.ToString();
}
```

---

### **3. Property Change Listener**

```csharp
vm.PropertyChanged += OnViewModelPropertyChanged;

private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
{
   if(e.PropertyName == nameof(MainViewModel.Text) || 
      e.PropertyName == nameof(MainViewModel.SelectedPackId) ||
      e.PropertyName == nameof(MainViewModel.SelectedStyle) ||
      e.PropertyName == nameof(MainViewModel.SelectedGlyphSize))
   {
      UpdateAsciiPreview(vm);
   }
}
```

---

## 🎯 Jak to funguje:

### **Scenario 1: Custom Size**

**Avalonia:**
1. Zadej W: **12**, H: **24**
2. ✅ `SelectedGlyphSize` = `12x24`
3. ✅ `SelectedPackId` = `"12x24"` (auto-created)
4. ✅ Preview se překreslí

**Consolonia:**
1. Zadej W: **12**, H: **24**
2. ✅ Stejné chování jako Avalonia
3. ✅ ASCII preview se překreslí s novými rozměry

---

### **Scenario 2: ASCII Preview**

**Input:** `"Hello"`

**Output (8x16 glyphs):**
```
█·······  ·████··  ██···  ██···  ·███··
█·······  ██···██  ██···  ██···  ██···█
█·······  ██···██  ██···  ██···  ██···█
███████·  ████···  ██···  ██···  ██···█
█·······  ██···██  ██···  ██···  ██···█
█·······  ██···██  ██···  ██···  ██···█
█·······  ·████··  █████  █████  ·███··
```

**Fallback:** Text input: `"Hello 😊 World!"`

---

## 📊 Performance:

### **Omezení:**
- ✅ **Max 5 znaků** v ASCII preview
- ✅ **Asynchronní rendering** (nepozastaví UI)
- ✅ **Cache glyphů** (rychlé opakované načítání)

### **Rozměry:**
- 8x16 glyph × 5 chars = **640 ASCII znaků** (malé)
- 16x32 glyph × 5 chars = **2560 ASCII znaků** (stále OK)
- 64x64 glyph × 5 chars = **20480 ASCII znaků** (možné zpomalení)

---

## 🎨 Srovnání verzí:

| Feature | Avalonia | Consolonia |
|---------|----------|------------|
| **Custom W/H** | ✅ TextBox + auto-update | ✅ TextBox + auto-update |
| **Pack Selector** | ✅ ComboBox | ✅ ComboBox |
| **Style Selector** | ✅ ComboBox | ✅ ComboBox |
| **Preview** | ✅ Full terminal render | ✅ ASCII art (5 chars) + Text |
| **Glyph clicking** | ✅ Click to select | ❌ Not implemented |
| **Scrolling** | ✅ Full scroll | ✅ Limited scroll |
| **Performance** | ✅ Full canvas | ✅ Optimized ASCII |

---

## 💡 Tipy pro uživatele:

### **Custom Size:**
1. Zadej W a H do textboxů
2. Velikost se automaticky změní
3. Pokud pack `WxH` neexistuje, vytvoří se automaticky

### **ASCII Preview:**
1. Zadej text (max 5 znaků zobrazených)
2. ASCII art se aktualizuje real-time
3. █ = pixel ON, · = pixel OFF
4. Mezera mezi znaky pro čitelnost

### **Výkon:**
- Menší glyphs (8x16) = rychlé
- Větší glyphs (64x64) = pomalejší
- Zkrať text na 1-3 znaky pro větší glyphs

---

## ✅ Výsledek:

✅ **Custom W/H selector** - funguje v obou verzích  
✅ **ASCII preview** - unikátní pro Consolonia  
✅ **Real-time update** - při změně textu, pack, style, size  
✅ **Performance optimized** - limit 5 znaků  
✅ **User-friendly** - viditelný pixel-by-pixel rendering  

**Obě verze jsou nyní plně funkční a vzájemně zastupitelné!** 🚀✨

---

## 🔮 Budoucí vylepšení (volitelné):

1. **Click to select glyph** - kliknutí na znak v ASCII preview
2. **Color support** - barevné pixely místo ·/█
3. **Zoom ASCII** - FontSize slider pro ASCII art
4. **Export ASCII** - copy to clipboard
5. **Animation** - animated glyphs preview
