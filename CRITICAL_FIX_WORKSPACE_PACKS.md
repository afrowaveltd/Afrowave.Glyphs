# Critical Fix - Workspace, Pack & Style Loading

## 🐛 Zjištěné problémy:

### 1. **Workspace picker crashes**
- ✅ **OPRAVENO**: Hide/Show main window před/po file pickeru

### 2. **Pack a Style combobox jsou prázdné po workspace změně**
- ✅ **OPRAVENO**: Pořadí inicializace - ReloadAsync() před nastavením SelectedPackId/SelectedStyle

### 3. **Chybějící custom W/H size selector**
- ⚠️ **TODO**: Přidat UI pro custom size input (viz níže)

---

## 🔧 Hlavní oprava:

### **Problém:**
```csharp
// ❌ ŠPATNĚ - nastavení hodnot PŘED načtením collections
var vm = new MainViewModel(packs, repo)
{
   SelectedPackId = "8x16",      // PackIds je prázdné!
   SelectedStyle = new FontStyleId("_base_")  // Styles je prázdné!
};

await vm.ReloadAsync();  // Až TEĎ se naplní PackIds a Styles
```

### **Řešení:**
```csharp
// ✅ SPRÁVNĚ - uložit hodnoty, načíst collections, PAK nastavit
var oldPackId = old?.SelectedPackId ?? "8x16";
var oldStyle = old?.SelectedStyle ?? new FontStyleId("_base_");

var vm = new MainViewModel(packs, repo)
{
   Text = oldText  // Pouze text, ne PackId/Style!
};

// ... setup handlers ...

await vm.ReloadAsync();  // NEJDŘÍV načíst PackIds a Styles

// PAK nastavit hodnoty
if(vm.PackIds.Contains(oldPackId))
   vm.SelectedPackId = oldPackId;
else if(vm.PackIds.Count > 0)
   vm.SelectedPackId = vm.PackIds[0];
```

---

## ✅ Opraveno v obou verzích:

### **Editor.Avalonia/MainWindow.axaml.cs**
- ✅ Řádky 390-415: `ReinitializeForWorkspaceAsync()`
- ✅ Pořadí: Save values → Create VM → ReloadAsync() → Set values

### **Editor.Consolonia/MainWindow.axaml.cs**
- ✅ Řádky 332-367: `ReinitializeForWorkspaceAsync()`
- ✅ Identické chování jako Avalonia

---

## 📋 Co nyní funguje:

### ✅ **Po změně workspace:**
1. Main window se skryje (Consolonia workaround)
2. FilePickerWindow se otevře
3. User vybere workspace folder
4. Workspace se uloží do settings
5. Symbols struktura se vytvoří
6. **ReloadAsync() načte PackIds a Styles z nového workspace**
7. **ComboBoxes se naplní správnými hodnotami**
8. Předchozí selection se obnoví (pokud existuje)
9. Main window se zobrazí zpět

### ✅ **Pack a Style ComboBoxes:**
- Zobrazují všechny packy z workspace (`8x16`, `16x16`, atd.)
- Zobrazují všechny styly (`_base_`, `bold`, `italic`, atd.)
- Zachovávají selection po workspace změně
- Fallback na první položku, pokud old selection neexistuje

---

## ⚠️ Zbývající TODO:

### **Custom W/H Size Selector** (zatím chybí v obou verzích)

**Návrh UI pro Avalonia:**
```xml
<StackPanel Orientation="Horizontal">
  <TextBlock Text="Size:" />
  <ComboBox ItemsSource="{Binding GlyphSizePresets}" 
            SelectedItem="{Binding SelectedGlyphSize}" />
  <TextBlock Text="Custom:" />
  <NumericUpDown Value="{Binding CustomWidth}" Minimum="1" Maximum="128" />
  <TextBlock Text="x" />
  <NumericUpDown Value="{Binding CustomHeight}" Minimum="1" Maximum="128" />
  <Button Content="Set" Command="{Binding SetCustomSizeCommand}" />
</StackPanel>
```

**Návrh UI pro Consolonia:**
```xml
<StackPanel Orientation="Horizontal">
  <TextBlock Text="W:" />
  <TextBox Text="{Binding CustomWidth}" Width="4" />
  <TextBlock Text="H:" />
  <TextBox Text="{Binding CustomHeight}" Width="4" />
  <Button Content="Set" Click="OnSetCustomSize" />
</StackPanel>
```

---

## 🎯 Testování:

### **Scenario 1: Změna workspace**
1. Spusť aplikaci
2. Klikni "Workspace"
3. Vyber jinou složku
4. ✅ **Pack a Style comboboxes by měly být naplněné**
5. ✅ **Preview by měl fungovat**

### **Scenario 2: První spuštění**
1. Smaž `appsettings.json`
2. Spusť aplikaci
3. ✅ **Automaticky vytvoří default workspace**
4. ✅ **Pack a Style by měly obsahovat default hodnoty**

### **Scenario 3: Import fontu**
1. Importuj font
2. Změň workspace
3. Vrať se zpět
4. ✅ **Importované packy by měly být viditelné**

---

## 📝 Poznámky:

### **Proč to předtím nefungovalo:**

1. **MainViewModel constructor** nastavoval `SelectedPackId` a `SelectedStyle`
2. **PackIds a Styles collections** byly v té době **prázdné**
3. **ComboBox binding** selhal (nelze nastavit hodnotu, která není v kolekci)
4. **ReloadAsync()** se volal později, ale binding už byl rozbitý

### **Jak to funguje teď:**

1. **ViewModel se vytvoří** bez nastavení PackId/Style
2. **ReloadAsync()** naplní PackIds a Styles z workspace
3. **TEĎ teprve** se nastaví SelectedPackId a SelectedStyle
4. **ComboBox binding** funguje, protože hodnoty jsou v collections

---

## 🚀 Výsledek:

✅ **Obě verze (Avalonia + Consolonia) jsou nyní synchronizované**  
✅ **Workspace picker funguje bez crashů**  
✅ **Pack a Style se správně načítají**  
✅ **Selection se zachovává při změně workspace**  

⚠️ **TODO**: Custom W/H size selector (lze přidat později)
