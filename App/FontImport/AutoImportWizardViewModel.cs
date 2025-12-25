using Core.Models;
using Storage.Abstractions.Abstractions;
using Storage.Abstractions.Models;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Tools.FontImport
{
    public class AutoImportWizardViewModel : INotifyPropertyChanged
    {
        private readonly FontImportService _importService;
        private string _fontPath = "";
        private string _packId = "8x16";
        private GridSize _glyphSize = new GridSize(8, 16);
        private string _status = "Ready to import";
        private string _currentRangeText = "";
        private int _progressPercent;
        private int _importedCount;
        private int _skippedCount;

        public event PropertyChangedEventHandler? PropertyChanged;

        public AutoImportWizardViewModel(IGlyphRepository repo, FontGlyphRasterizer rasterizer)
        {
            _importService = new FontImportService(repo, rasterizer);
        }

        public string FontPath
        {
            get => _fontPath;
            set => Set(ref _fontPath, value);
        }

        public string PackId
        {
            get => _packId;
            set => Set(ref _packId, value);
        }

        public GridSize GlyphSize
        {
            get => _glyphSize;
            set
            {
                if (Set(ref _glyphSize, value))
                {
                    Notify(nameof(GlyphSizeText));
                }
            }
        }

        public string GlyphSizeText => $"{GlyphSize.Width}x{GlyphSize.Height}";

        public string Status
        {
            get => _status;
            set => Set(ref _status, value);
        }

        public string CurrentRangeText
        {
            get => _currentRangeText;
            set => Set(ref _currentRangeText, value);
        }

        public int ProgressPercent
        {
            get => _progressPercent;
            set => Set(ref _progressPercent, value);
        }

        public int ImportedCount
        {
            get => _importedCount;
            set => Set(ref _importedCount, value);
        }

        public int SkippedCount
        {
            get => _skippedCount;
            set => Set(ref _skippedCount, value);
        }

        public async Task StartImportAsync(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(FontPath))
            {
                Status = "Error: No font file selected";
                return;
            }

            Status = "Starting import...";
            ProgressPercent = 0;
            ImportedCount = 0;
            SkippedCount = 0;

            var progress = new Progress<ImportProgress>(p =>
            {
                CurrentRangeText = $"Range: {p.CurrentRange} ({p.RangeProgress}/{p.TotalRanges}) - Current: '{p.CurrentChar}'";
                ProgressPercent = (int)((double)p.RangeProgress / p.TotalRanges * 100);
                ImportedCount = p.ImportedCount;
                SkippedCount = p.SkippedCount;
                Status = $"Importing from {p.CurrentRange}...";
            });

            try
            {
                var style = new FontStyleId("_base_");
                var result = await _importService.ImportAllAvailableAsync(
                    PackId,
                    style,
                    FontPath,
                    GlyphSize,
                    progress,
                    ct);

                Status = $"Complete! Imported {result.Imported} glyphs, skipped {result.Skipped}";
                ProgressPercent = 100;
            }
            catch (OperationCanceledException)
            {
                Status = "Import cancelled by user";
                throw;
            }
            catch (Exception ex)
            {
                Status = $"Error: {ex.Message}";
            }
        }

        private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            Notify(name);
            return true;
        }

        private void Notify([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
