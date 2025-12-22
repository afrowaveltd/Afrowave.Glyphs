using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Core.Naming;
using Editor.Avalonia.Services;
using Storage.Abstractions.Abstractions;
using Storage.FileSystem;
using System;
using System.Threading.Tasks;
using Tools;


namespace Editor.Avalonia;

public partial class App : Application
{
   public override void Initialize()
   {
      AvaloniaXamlLoader.Load(this);
   }

   public override void OnFrameworkInitializationCompleted()
   {
      if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
      {
         // Show a splash screen to keep the app alive while initializing
         var splash = new Window
         {
            Content = new TextBlock
            {
               Text = "Loading...",
               HorizontalAlignment = HorizontalAlignment.Center,
               VerticalAlignment = VerticalAlignment.Center
            },
            Width = 300,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            SystemDecorations = SystemDecorations.BorderOnly,
            Title = "Loading"
         };

         desktop.MainWindow = splash;

         // Async init
         _ = InitAsync(desktop, splash);
      }

      base.OnFrameworkInitializationCompleted();
   }

   private async Task InitAsync(IClassicDesktopStyleApplicationLifetime desktop, Window splash)
   {
      try
      {
         // 1) Load settings
         var store = new JsonAppSettingsStore();
         var settingsService = new SettingsService(store);
         await settingsService.InitializeAsync().ConfigureAwait(true);

         var settings = settingsService.Current;
         string symbolsRoot = settings.GetActiveSymbolsRoot();

         // 2) Create FS storage services (repo + pack provider)
         IGlyphRepository repo = CreateRepository(symbolsRoot);
         IFontPackProvider packs = CreatePackProvider(symbolsRoot);

         // 3) ViewModel (sdíletelný i s Consolonia)
         var vm = new MainViewModel(packs, repo);

         // přeneseme uložené hodnoty ze settings do VM
         vm.Text = settings.LastText;
         vm.SelectedPackId = settings.LastPackId;
         vm.SelectedStyle = settings.LastStyle;

         // načteme packy + styly + render
         await vm.ReloadAsync().ConfigureAwait(true);

         // 4) Terminal cache pro Avalonia control
         var cache = new TerminalGlyphCache(repo);

         // 5) Window
         var window = new MainWindow();
         window.DataContext = vm;
         window.SetTerminalCache(cache);

         desktop.MainWindow = window;
         window.Show();
         splash.Close();
      }
      catch(Exception ex)
      {
         if(splash.Content is TextBlock tb)
         {
            tb.Text = $"Error: {ex.Message}";
            tb.Foreground = Brushes.Red;
         }
      }
   }

   private static IGlyphRepository CreateRepository(string symbolsRoot)
   {
      var options = new FileSystemOptions(symbolsRoot);

      // Default naming: UTF / internal / custom (_afw_a apod.)
      IGlyphNaming naming = new DefaultGlyphNaming();

      return new FileSystemGlyphRepository(options, naming);
   }

   private static IFontPackProvider CreatePackProvider(string symbolsRoot)
   {
      var options = new FileSystemOptions(symbolsRoot);
      return new FileSystemFontPackProvider(options);
   }

}