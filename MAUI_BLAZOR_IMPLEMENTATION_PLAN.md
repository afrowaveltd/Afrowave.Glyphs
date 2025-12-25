# MAUI + Blazor Hybrid - Implementační plán

## 🎯 **Cíl:**
Vytvořit mobilní/tablet verzi Glyph Editoru s Blazor UI

## 🏗️ **Projekt struktura:**

```
Editor.Maui/
  ├── Platforms/
  │   ├── Android/
  │   │   ├── MainActivity.cs
  │   │   ├── AndroidManifest.xml
  │   │   └── Resources/
  │   ├── iOS/
  │   │   ├── AppDelegate.cs
  │   │   └── Info.plist
  │   └── MacCatalyst/
  │       ├── AppDelegate.cs
  │       └── Info.plist
  │
  ├── Components/              🔥 Blazor Components
  │   ├── Layout/
  │   │   ├── MainLayout.razor
  │   │   └── NavMenu.razor
  │   ├── Pages/
  │   │   ├── Index.razor      # Home page
  │   │   ├── Editor.razor     # Glyph editor
  │   │   ├── Browser.razor    # Browse glyphs
  │   │   └── Settings.razor   # Settings
  │   ├── Shared/
  │   │   ├── PackSelector.razor
  │   │   ├── StyleSelector.razor
  │   │   ├── GlyphPixelGrid.razor  # Touch-based!
  │   │   └── Preview.razor
  │   └── _Imports.razor
  │
  ├── Services/
  │   ├── MauiWorkspaceService.cs
  │   ├── TouchGlyphEditorService.cs
  │   ├── CloudStorageService.cs
  │   └── PlatformFontService.cs
  │
  ├── wwwroot/
  │   ├── css/
  │   │   ├── app.css
  │   │   └── bootstrap/
  │   ├── js/
  │   │   └── touch-handler.js
  │   └── index.html
  │
  ├── MauiProgram.cs
  └── Editor.Maui.csproj
```

---

## 📝 **Krok 1: Vytvoření MAUI projektu**

### **Editor.Maui.csproj:**

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFrameworks>net10.0-android;net10.0-ios;net10.0-maccatalyst</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <RootNamespace>Editor.Maui</RootNamespace>
    <UseMaui>true</UseMaui>
    <SingleProject>true</SingleProject>
    <EnableDefaultCssItems>false</EnableDefaultCssItems>
    
    <!-- Display name -->
    <ApplicationTitle>Afrowave Glyph Editor</ApplicationTitle>
    <ApplicationId>com.afrowave.glypheditor</ApplicationId>
    <ApplicationVersion>1</ApplicationVersion>
    
    <!-- Android -->
    <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'">21.0</SupportedOSPlatformVersion>
    
    <!-- iOS -->
    <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'">14.2</SupportedOSPlatformVersion>
    
    <!-- macOS -->
    <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'maccatalyst'">14.0</SupportedOSPlatformVersion>
  </PropertyGroup>

  <ItemGroup>
    <!-- MAUI -->
    <PackageReference Include="Microsoft.Maui.Controls" Version="10.0.0" />
    <PackageReference Include="Microsoft.Maui.Controls.Compatibility" Version="10.0.0" />
    
    <!-- Blazor Hybrid -->
    <PackageReference Include="Microsoft.AspNetCore.Components.WebView.Maui" Version="10.0.0" />
    
    <!-- Project references -->
    <ProjectReference Include="..\Core\Core.csproj" />
    <ProjectReference Include="..\Storage.Abstractions\Storage.Abstractions.csproj" />
    <ProjectReference Include="..\Storage.FileSystem\Storage.FileSystem.csproj" />
    <ProjectReference Include="..\Tools\Tools.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- Blazor -->
    <BlazorWebView Include="wwwroot\**" />
  </ItemGroup>
</Project>
```

---

## 📝 **Krok 2: MauiProgram.cs**

```csharp
using Microsoft.Extensions.Logging;
using Editor.Maui.Services;
using Tools;
using Storage.FileSystem;
using Storage.Abstractions.Abstractions;
using Core.Naming;

namespace Editor.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Blazor Hybrid
        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // Our services
        builder.Services.AddSingleton<IWorkspaceService, MauiWorkspaceService>();
        builder.Services.AddSingleton<IGlyphNaming, DefaultGlyphNaming>();
        
        // Transient because path may change
        builder.Services.AddTransient<IGlyphRepository>(sp =>
        {
            var workspace = sp.GetRequiredService<IWorkspaceService>();
            var naming = sp.GetRequiredService<IGlyphNaming>();
            var symbolsRoot = workspace.GetSymbolsRoot();
            return new FileSystemGlyphRepository(
                new FileSystemOptions(symbolsRoot), 
                naming);
        });
        
        builder.Services.AddTransient<IFontPackProvider>(sp =>
        {
            var workspace = sp.GetRequiredService<IWorkspaceService>();
            var symbolsRoot = workspace.GetSymbolsRoot();
            return new FileSystemFontPackProvider(
                new FileSystemOptions(symbolsRoot));
        });
        
        // Platform-specific
        builder.Services.AddSingleton<IPlatformFontService, PlatformFontService>();
        builder.Services.AddSingleton<ICloudStorageService, CloudStorageService>();
        
        // ViewModels
        builder.Services.AddTransient<MainViewModel>();

        return builder.Build();
    }
}
```

---

## 📝 **Krok 3: Blazor Components**

### **Components/Pages/Index.razor:**

```razor
@page "/"
@inject IFontPackProvider PackProvider
@inject IGlyphRepository Repository
@inject NavigationManager Navigation

<PageTitle>Glyph Editor</PageTitle>

<div class="container-fluid">
    <div class="row">
        <div class="col-12">
            <h1>🎨 Afrowave Glyph Editor</h1>
        </div>
    </div>

    <div class="row mt-3">
        <div class="col-md-6 col-12">
            <PackSelector @bind-SelectedPack="selectedPack" />
        </div>
        <div class="col-md-6 col-12">
            <StyleSelector Pack="@selectedPack" @bind-SelectedStyle="selectedStyle" />
        </div>
    </div>

    <div class="row mt-3">
        <div class="col-12">
            <div class="btn-group" role="group">
                <button class="btn btn-primary" @onclick="NavigateToEditor">
                    ✏️ Edit Glyph
                </button>
                <button class="btn btn-secondary" @onclick="NavigateToBrowser">
                    🔍 Browse Glyphs
                </button>
                <button class="btn btn-info" @onclick="NavigateToImport">
                    📥 Import Font
                </button>
            </div>
        </div>
    </div>

    <div class="row mt-4">
        <div class="col-12">
            <Preview Pack="@selectedPack" Style="@selectedStyle" />
        </div>
    </div>
</div>

@code {
    private string? selectedPack;
    private string? selectedStyle;

    private void NavigateToEditor()
    {
        Navigation.NavigateTo($"/editor?pack={selectedPack}&style={selectedStyle}");
    }

    private void NavigateToBrowser()
    {
        Navigation.NavigateTo($"/browser?pack={selectedPack}&style={selectedStyle}");
    }

    private void NavigateToImport()
    {
        Navigation.NavigateTo("/import");
    }
}
```

---

### **Components/Shared/GlyphPixelGrid.razor:**

```razor
@using Core.Models
@inject IJSRuntime JS

<div class="pixel-grid-container">
    <canvas @ref="canvasRef" 
            width="@(Width * PixelSize)" 
            height="@(Height * PixelSize)"
            @ontouchstart="OnTouchStart"
            @ontouchmove="OnTouchMove"
            @ontouchend="OnTouchEnd"
            class="pixel-grid-canvas">
    </canvas>

    <div class="pixel-grid-tools">
        <button class="btn btn-sm btn-secondary" @onclick="Undo">↶ Undo</button>
        <button class="btn btn-sm btn-secondary" @onclick="Redo">↷ Redo</button>
        <button class="btn btn-sm btn-danger" @onclick="Clear">🗑️ Clear</button>
    </div>
</div>

@code {
    [Parameter] public GlyphBitmap? Bitmap { get; set; }
    [Parameter] public EventCallback<GlyphBitmap> OnBitmapChanged { get; set; }
    [Parameter] public int Width { get; set; } = 8;
    [Parameter] public int Height { get; set; } = 16;
    [Parameter] public int PixelSize { get; set; } = 32; // Large for touch!

    private ElementReference canvasRef;
    private bool isDrawing = false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && Bitmap != null)
        {
            await RenderBitmap();
        }
    }

    private async Task OnTouchStart(TouchEventArgs e)
    {
        isDrawing = true;
        await HandleTouch(e);
    }

    private async Task OnTouchMove(TouchEventArgs e)
    {
        if (isDrawing)
        {
            await HandleTouch(e);
        }
    }

    private void OnTouchEnd(TouchEventArgs e)
    {
        isDrawing = false;
    }

    private async Task HandleTouch(TouchEventArgs e)
    {
        if (e.Touches.Length == 0 || Bitmap == null) return;

        var touch = e.Touches[0];
        var bounds = await JS.InvokeAsync<BoundingClientRect>(
            "getBoundingClientRect", canvasRef);

        var x = (int)((touch.ClientX - bounds.Left) / PixelSize);
        var y = (int)((touch.ClientY - bounds.Top) / PixelSize);

        if (x >= 0 && x < Width && y >= 0 && y < Height)
        {
            var pixel = Bitmap.GetPixel(x, y);
            Bitmap.SetPixel(x, y, !pixel);
            
            await RenderBitmap();
            await OnBitmapChanged.InvokeAsync(Bitmap);
        }
    }

    private async Task RenderBitmap()
    {
        if (Bitmap == null) return;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var pixel = Bitmap.GetPixel(x, y);
                var color = pixel ? "#000000" : "#FFFFFF";
                
                await JS.InvokeVoidAsync("drawPixel", canvasRef, 
                    x * PixelSize, y * PixelSize, PixelSize, color);
            }
        }
    }

    private async Task Undo()
    {
        // TODO: Implement undo stack
        await Task.CompletedTask;
    }

    private async Task Redo()
    {
        // TODO: Implement redo stack
        await Task.CompletedTask;
    }

    private async Task Clear()
    {
        if (Bitmap == null) return;
        
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Bitmap.SetPixel(x, y, false);
            }
        }
        
        await RenderBitmap();
        await OnBitmapChanged.InvokeAsync(Bitmap);
    }
}
```

---

### **wwwroot/js/touch-handler.js:**

```javascript
window.getBoundingClientRect = (element) => {
    return element.getBoundingClientRect();
};

window.drawPixel = (canvas, x, y, size, color) => {
    const ctx = canvas.getContext('2d');
    ctx.fillStyle = color;
    ctx.fillRect(x, y, size, size);
    
    // Grid lines
    ctx.strokeStyle = '#CCCCCC';
    ctx.strokeRect(x, y, size, size);
};
```

---

### **wwwroot/css/app.css:**

```css
.pixel-grid-container {
    display: flex;
    flex-direction: column;
    align-items: center;
    padding: 1rem;
}

.pixel-grid-canvas {
    border: 2px solid #333;
    touch-action: none; /* Prevent scrolling while drawing */
    cursor: crosshair;
}

.pixel-grid-tools {
    margin-top: 1rem;
    display: flex;
    gap: 0.5rem;
}

/* Touch-friendly buttons */
.btn {
    min-width: 80px;
    min-height: 50px;
    font-size: 1.1rem;
}

/* Responsive */
@media (max-width: 768px) {
    .pixel-grid-canvas {
        max-width: 100%;
    }
    
    .btn {
        min-width: 60px;
        min-height: 40px;
    }
}
```

---

## 📋 **Krok 4: Platform-specific Services**

### **Services/PlatformFontService.cs:**

```csharp
#if ANDROID
using Android.Graphics;
#elif IOS || MACCATALYST
using UIKit;
#endif

namespace Editor.Maui.Services;

public interface IPlatformFontService
{
    Task<List<string>> GetSystemFontsAsync();
    Task<byte[]?> GetFontDataAsync(string fontName);
}

public class PlatformFontService : IPlatformFontService
{
    public async Task<List<string>> GetSystemFontsAsync()
    {
        var fonts = new List<string>();

#if ANDROID
        // Android system fonts
        var fontDir = new Java.IO.File("/system/fonts");
        if (fontDir.Exists())
        {
            foreach (var file in fontDir.ListFiles())
            {
                if (file.Name.EndsWith(".ttf") || file.Name.EndsWith(".otf"))
                {
                    fonts.Add(file.AbsolutePath);
                }
            }
        }
#elif IOS || MACCATALYST
        // iOS system fonts
        foreach (var familyName in UIFont.FamilyNames)
        {
            fonts.Add(familyName);
        }
#endif

        return await Task.FromResult(fonts);
    }

    public async Task<byte[]?> GetFontDataAsync(string fontName)
    {
#if ANDROID
        if (File.Exists(fontName))
        {
            return await File.ReadAllBytesAsync(fontName);
        }
#elif IOS || MACCATALYST
        // iOS font loading
        var font = UIFont.FromName(fontName, 12);
        if (font != null)
        {
            // TODO: Extract font data from UIFont
        }
#endif

        return null;
    }
}
```

---

## 📋 **Krok 5: Cloud Storage**

### **Services/CloudStorageService.cs:**

```csharp
namespace Editor.Maui.Services;

public interface ICloudStorageService
{
    Task<bool> ExportWorkspaceAsync(string workspacePath, string cloudPath);
    Task<bool> ImportWorkspaceAsync(string cloudPath, string localPath);
}

public class CloudStorageService : ICloudStorageService
{
    public async Task<bool> ExportWorkspaceAsync(string workspacePath, string cloudPath)
    {
        // Create ZIP
        var zipPath = Path.Combine(Path.GetTempPath(), "workspace.zip");
        System.IO.Compression.ZipFile.CreateFromDirectory(workspacePath, zipPath);

        // TODO: Upload to Google Drive / Dropbox / iCloud
        // For now, just save to device storage
        var documentsPath = FileSystem.AppDataDirectory;
        var targetPath = Path.Combine(documentsPath, "exports", cloudPath);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        File.Copy(zipPath, targetPath, true);

        return await Task.FromResult(true);
    }

    public async Task<bool> ImportWorkspaceAsync(string cloudPath, string localPath)
    {
        // TODO: Download from cloud
        // For now, extract from local storage
        var documentsPath = FileSystem.AppDataDirectory;
        var sourcePath = Path.Combine(documentsPath, "exports", cloudPath);
        
        if (File.Exists(sourcePath))
        {
            System.IO.Compression.ZipFile.ExtractToDirectory(sourcePath, localPath, true);
            return await Task.FromResult(true);
        }

        return await Task.FromResult(false);
    }
}
```

---

## ✅ **Benefits MAUI + Blazor:**

1. ✅ **Web technologie** - HTML/CSS/JavaScript familiarity
2. ✅ **Hot reload** - Instant UI updates
3. ✅ **Responsive** - Bootstrap for mobile/tablet layouts
4. ✅ **Touch-optimized** - Large buttons, gestures
5. ✅ **Shared logic** - 80% code reuse from Desktop
6. ✅ **Future web version** - Same components for web app!

---

## 🎯 **Roadmap:**

### **Week 1:**
- [ ] Create Editor.Maui project
- [ ] Setup MauiProgram + Blazor
- [ ] Basic UI (Index.razor)

### **Week 2:**
- [ ] GlyphPixelGrid component (touch)
- [ ] PackSelector, StyleSelector
- [ ] Preview component

### **Week 3:**
- [ ] Android platform services
- [ ] Font import from system
- [ ] Storage service

### **Week 4:**
- [ ] iOS support
- [ ] Cloud sync
- [ ] Polish & testing

---

**MAUI + Blazor = Best of both worlds!** 🚀📱

**Native performance + Web development speed!**
