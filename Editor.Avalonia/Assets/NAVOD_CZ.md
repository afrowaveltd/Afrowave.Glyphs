# 🎨 NÁVOD: Jak vytvořit ikonu v GIMPu

## Rychlý přehled:

1. ✅ Otevři `icon-template.svg` v GIMPu (256×256 px)
2. ✅ Export 4 PNG soubory (256, 48, 32, 16)
3. ✅ Zkombinuj do `icon.ico` (online nástroj)
4. ✅ Přejmenuj `icon-256.png` → `icon.png`
5. ✅ Odkomentuj řádky v `Editor.Avalonia.csproj`

---

## Krok 1: GIMP Export

### Otevři SVG:
```
File → Open → icon-template.svg
Velikost: 256×256 pixels
```

### Export PNG soubory:

**První export (256×256):**
```
File → Export As...
Název: icon-256.png
Formát: PNG
✅ Flatten image
```

**Druhý export (48×48):**
```
Image → Scale Image...
Šířka: 48
Výška: 48
Interpolace: Cubic

File → Export As...
Název: icon-48.png
```

**Třetí export (32×32):**
```
Image → Scale Image...
Šířka: 32
Výška: 32

File → Export As...
Název: icon-32.png
```

**Čtvrtý export (16×16):**
```
Image → Scale Image...
Šířka: 16
Výška: 16

File → Export As...
Název: icon-16.png
```

---

## Krok 2: Vytvoř ICO soubor

### Online nástroj (NEJJEDNODUŠŠÍ):

1. Jdi na: **https://convertio.co/cs/png-ico/**
2. Nahraj všechny 4 PNG soubory
3. Nastav: "Vícenásobná velikost" (Multi-size)
4. Klikni "Převést"
5. Stáhni výsledný `icon.ico`

---

## Krok 3: Zkopíruj soubory do projektu

```
1. Zkopíruj icon.ico → Editor.Avalonia\Assets\icon.ico
2. Přejmenuj icon-256.png → icon.png
3. Zkopíruj icon.png → Editor.Avalonia\Assets\icon.png
```

---

## Krok 4: Aktivuj ikony v projektu

Otevři `Editor.Avalonia\Editor.Avalonia.csproj` a **odkomentuj** tyto řádky:

**Najdi:**
```xml
<!-- <ApplicationIcon>Assets\icon.ico</ApplicationIcon> -->
```

**Změň na:**
```xml
<ApplicationIcon>Assets\icon.ico</ApplicationIcon>
```

**A najdi:**
```xml
<!--
<ItemGroup>
  <AvaloniaResource Include="Assets\icon.png" />
</ItemGroup>
-->
```

**Změň na:**
```xml
<ItemGroup>
  <AvaloniaResource Include="Assets\icon.png" />
</ItemGroup>
```

---

## Krok 5: Rebuild projektu

```sh
dotnet build
```

✅ **Hotovo!** Ikona se objeví v:
- EXE souboru
- Taskbaru
- Alt+Tab
- File Exploreru

---

## 🎨 Barevné schéma:

- **Pozadí:** Tmavě šedá (#1a1a1a)
- **Hlavní "A":** Hercules zelená (#00ff00)
- **Akcenty:** Oranžová/Žlutá (#ff8800, #ffaa00)
- **Mřížka:** Tmavší zelená (#00aa00)

---

## 💡 Tipy:

- Při exportu malých velikostí (16×16, 32×32) použij **Cubic interpolation**
- Zkontroluj, že všechny PNG mají **transparentní pozadí** (ne černé)
- SVG template (`icon-template.svg`) si **nech** pro budoucí úpravy

---

🎉 **Máš hotovo!**
