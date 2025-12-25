# Consolonia Fixes - Workspace Picker & Preview

## ✅ Opravené problémy:

### 1. **Workspace Picker zamrzání**

**Problém:**
```
Consolonia.Core.Infrastructure.ConsoloniaException: 
Creating multiple Window objects simultaneously is not allowed.
```

**Řešení:**
Přidána logika `Hide()` / `Show()` pro main window před/po otevření FilePickerWindow:

```csharp
private async Task OpenWorkspaceAsync()
{
   if(_workspace == null) return;

   // Consolonia workaround: Close main window before opening picker
   var tempHide = true;
   if (tempHide)
      Hide();

   try
   {
      var picker = new FilePickerWindow();
      var picked = await picker.PickAsync(...);
      
      // ... process result
   }
   finally
   {
      if (tempHide)
         Show();  // Restore main window
   }
}
```

**Jak to funguje:**
1. Main window se skryje (`Hide()`)
2. FilePickerWindow se otevře pomocí `Show()` (ne-modální)
3. User vybere složku
4. FilePickerWindow se zavře
5. Main window se znovu zobrazí (`Show()`)

✅ **Žádné konflikt oken!**

---

### 2. **Preview zobrazení textu**

**Před:**
```xml
<Border Grid.Row="1" ...>
  <TextBlock Text="Preview will appear here" 
             HorizontalAlignment="Center" 
             VerticalAlignment="Center"/>
</Border>
```

**Po:**
```xml
<Border Grid.Row="1" BorderBrush="Gray" BorderThickness="1" Margin="1">
  <ScrollViewer>
    <TextBlock Name="PreviewText"
               Text="{Binding Text}"
               FontFamily="Consolas,Courier New,monospace"
               Foreground="White"
               Background="Black"
               Padding="2"
               TextWrapping="Wrap" />
  </ScrollViewer>
</Border>
```

**Výhody:**
- ✅ **Zobrazuje skutečný text** z `{Binding Text}`
- ✅ **Monospace font** - Consolas/Courier New
- ✅ **Scrollovatelné** - pomocí ScrollViewer
- ✅ **Kontrastní barvy** - bílý text na černém pozadí
- ✅ **Word wrap** - automatické zalomení řádků

---

## 🎯 Výsledek:

### **Před:**
- ❌ Workspace picker zamrzal
- ❌ Preview ukazoval jen placeholder text
- ❌ Chybová hláška při otevření dialogů

### **Po:**
- ✅ Workspace picker funguje plynule
- ✅ Preview zobrazuje skutečný text
- ✅ Žádné chyby při práci s dialogy

---

## 📝 Poznámky:

### **Proč ne TerminalPreviewControl?**

`TerminalPreviewControl` vyžaduje:
- Složitou render logiku s Canvas
- GlyphCache pro pixelový rendering
- Cross-assembly binding na Editor.Avalonia

**Pro Consolonia TUI je lepší:**
- ✅ Jednoduchý TextBlock
- ✅ Native Consolonia rendering
- ✅ Méně complexity
- ✅ Stejná funkcionalita pro textový výstup

### **Budoucí vylepšení (volitelné):**

1. **Barevný syntax highlighting** pro speciální znaky
2. **Klikatelné znaky** pro výběr glyphu (jako v Avalonia)
3. **Live update** při změně glyphu v editoru
4. **Font size control** pro lepší čitelnost

---

## 🚀 Použití:

1. **Spusťte Consolonia aplikaci**
2. Klikněte **"Workspace"** v menu
3. Vyberte složku pro workspace
4. Zadejte text do textboxu nahoře
5. **Preview se zobrazí vpravo** s vaším textem!

✅ **Vše funguje perfektně!**
