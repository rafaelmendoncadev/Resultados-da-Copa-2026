using Android.App;
using Android.OS;
using Android.Views;
using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;
using AndroidX.SwipeRefreshLayout.Widget;
using Google.Android.Material.Chip;
using Resultados_da_Copa_2026.Adapters;
using Resultados_da_Copa_2026.Helpers;
using Resultados_da_Copa_2026.Models;
using Resultados_da_Copa_2026.Services;

namespace Resultados_da_Copa_2026.Fragments;

public class KnockoutFragment : AndroidX.Fragment.App.Fragment
{
    private MatchRepository? _repository;
    private GameListAdapter? _adapter;
    private SwipeRefreshLayout? _swipeRefresh;
    private RecyclerView? _recyclerView;
    private ProgressBar? _progressBar;
    private View? _errorLayout;
    private Chip? _liveChip;
    private global::Google.Android.Material.Button.MaterialButton? _datePickerButton;
    private List<Game> _knockoutGames = [];
    private DateTime? _selectedDate;
    private bool _showLiveOnly;

    // Auto-refresh para jogos ao vivo
    private CancellationTokenSource? _refreshCts;
    private const int LiveRefreshIntervalMs = 60_000; // 60 segundos

    private const int CopaYear = 2026;
    private const int CopaMonthStart = 6;
    private const int CopaMonthEnd = 7;

    public event Action<DataResult<List<Game>>>? DataLoaded;

    private static readonly MatchStage[] KnockoutStages =
    [
        MatchStage.RoundOf32,
        MatchStage.RoundOf16,
        MatchStage.QuarterFinal,
        MatchStage.SemiFinal,
        MatchStage.ThirdPlace,
        MatchStage.Final
    ];

    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        return inflater.Inflate(Resource.Layout.fragment_knockout, container, false);
    }

    public override void OnViewCreated(View view, Bundle? savedInstanceState)
    {
        base.OnViewCreated(view, savedInstanceState);
        _repository = new MatchRepository(RequireContext());
        _swipeRefresh = view.FindViewById<SwipeRefreshLayout>(Resource.Id.swipeRefresh);
        _recyclerView = view.FindViewById<RecyclerView>(Resource.Id.recyclerView);
        _progressBar = view.FindViewById<ProgressBar>(Resource.Id.progressBar);
        _errorLayout = view.FindViewById(Resource.Id.errorLayout);
        _liveChip = view.FindViewById<Chip>(Resource.Id.liveChip);
        _datePickerButton = view.FindViewById<global::Google.Android.Material.Button.MaterialButton>(Resource.Id.datePickerButton);

        _adapter = new GameListAdapter();
        _adapter.ItemClick += game =>
        {
            var intent = new Android.Content.Intent(RequireContext(), typeof(Activities.GameDetailActivity));
            intent.PutExtra(Activities.GameDetailActivity.ExtraGameId, game.Id);
            StartActivity(intent);
        };
        _recyclerView!.SetLayoutManager(new LinearLayoutManager(RequireContext()));
        _recyclerView.SetAdapter(_adapter);

        if (_datePickerButton != null)
        {
            UpdateDateButtonText();
            _datePickerButton.Click += OnDatePickerButtonClick;
        }

        if (_liveChip != null)
        {
            _liveChip.Checked = _showLiveOnly;
            _liveChip.Click += (_, _) =>
            {
                _showLiveOnly = !_showLiveOnly;
                _liveChip.Checked = _showLiveOnly;
                if (_showLiveOnly)
                {
                    _selectedDate = null;
                    UpdateDateButtonText();
                }
                ApplyFilter();
            };
        }

        _swipeRefresh!.SetColorSchemeResources(Resource.Color.wc_green_dark);
        _swipeRefresh.Refresh += async (_, _) => await LoadDataAsync(forceRefresh: true);

        view.FindViewById<global::Google.Android.Material.Button.MaterialButton>(Resource.Id.retryButton)!
            .Click += async (_, _) => await LoadDataAsync(forceRefresh: true);

        _ = LoadDataAsync();
    }

    // ────────── Auto-refresh para jogos ao vivo ──────────

    public override void OnResume()
    {
        base.OnResume();
        StartAutoRefresh();
    }

    public override void OnPause()
    {
        base.OnPause();
        StopAutoRefresh();
    }

    public override void OnDestroyView()
    {
        base.OnDestroyView();
        StopAutoRefresh();
    }

    private void StartAutoRefresh()
    {
        StopAutoRefresh();
        _refreshCts = new CancellationTokenSource();
        _ = AutoRefreshLoop(_refreshCts.Token);
    }

    private void StopAutoRefresh()
    {
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
        _refreshCts = null;
    }

    private async Task AutoRefreshLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(LiveRefreshIntervalMs, ct);

                if (ct.IsCancellationRequested)
                    break;

                var hasLiveGames = _knockoutGames.Any(g => g.IsLive);

                if (hasLiveGames)
                {
                    // Atualização em tempo real com indicador visual
                    RunOnUi(() =>
                    {
                        if (_swipeRefresh != null && !_swipeRefresh.Refreshing)
                            _swipeRefresh.Refreshing = true;
                    });
                    await LoadDataAsync(forceRefresh: true);
                }
                else
                {
                    // Verificação silenciosa no cache para ver se novos jogos começaram
                    await RefreshFromCacheSilentAsync();
                }
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch
            {
                // Ignora erros de rede, continua o loop
            }
        }
    }

    /// <summary>
    /// Recarrega dados do cache sem mostrar indicador de carregamento.
    /// Se encontrar jogos ao vivo, aplica o filtro para que apareçam na lista.
    /// </summary>
    private async Task RefreshFromCacheSilentAsync()
    {
        try
        {
            var result = await _repository!.GetGamesAsync(RequireContext(), forceRefresh: false);
            var liveGames = result.Data
                .Where(g => KnockoutStages.Contains(g.Stage) && g.IsLive)
                .ToList();

            if (liveGames.Count > 0)
            {
                // Encontrou jogos ao vivo no cache — atualiza a lista e força refresh da API
                _knockoutGames = result.Data
                    .Where(g => KnockoutStages.Contains(g.Stage))
                    .OrderBy(g => g.Stage.SortOrder())
                    .ThenBy(g => int.TryParse(g.Id, out var id) ? id : 999)
                    .ToList();

                RunOnUi(() =>
                {
                    if (_swipeRefresh != null && !_swipeRefresh.Refreshing)
                        _swipeRefresh.Refreshing = true;
                });

                // Agora faz um force refresh para dados em tempo real
                await LoadDataAsync(forceRefresh: true);
            }
        }
        catch
        {
            // Silencia erros na verificação em background
        }
    }

    // ────────── Carregamento de dados ──────────

    private async Task LoadDataAsync(bool forceRefresh = false)
    {
        RunOnUi(() => ShowLoading(true));
        try
        {
            var result = await _repository!.GetGamesAsync(RequireContext(), forceRefresh);

            _knockoutGames = result.Data
                .Where(g => KnockoutStages.Contains(g.Stage))
                .OrderBy(g => g.Stage.SortOrder())
                .ThenBy(g => int.TryParse(g.Id, out var id) ? id : 999)
                .ToList();

            RunOnUi(() =>
            {
                ApplyFilter();

                DataLoaded?.Invoke(new DataResult<List<Game>>
                {
                    Data = _knockoutGames,
                    FromCache = result.FromCache,
                    CachedAt = result.CachedAt,
                    IsOffline = result.IsOffline
                });
                ShowError(_knockoutGames.Count == 0);
            });
        }
        catch
        {
            RunOnUi(() => ShowError(true));
        }
        finally
        {
            RunOnUi(() =>
            {
                ShowLoading(false);
                if (_swipeRefresh != null)
                    _swipeRefresh.Refreshing = false;
            });
        }
    }

    // ────────── DatePicker ──────────

    private void OnDatePickerButtonClick(object? sender, EventArgs e)
    {
        if (_selectedDate.HasValue)
        {
            ShowDatePickerWithClearOption();
            return;
        }

        ShowDatePicker();
    }

    private void ShowDatePicker()
    {
        var now = DateTime.Now;
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var initialYear = CopaYear;
        var initialMonth = now.Year == CopaYear && now.Month >= CopaMonthStart && now.Month <= CopaMonthEnd
            ? now.Month - 1
            : CopaMonthStart - 1;
        var initialDay = now.Year == CopaYear && (now.Month - 1) == initialMonth
            ? Math.Clamp(now.Day, 1, 30)
            : 11;

        var datePicker = new DatePicker(RequireContext());
        datePicker.UpdateDate(initialYear, initialMonth, initialDay);

        var minDate = new DateTime(CopaYear, CopaMonthStart, 1, 0, 0, 0, DateTimeKind.Utc);
        var maxDate = new DateTime(CopaYear, CopaMonthEnd, 31, 23, 59, 59, DateTimeKind.Utc);
        datePicker.MinDate = (long)(minDate - epoch).TotalMilliseconds;
        datePicker.MaxDate = (long)(maxDate - epoch).TotalMilliseconds;

        var builder = new AlertDialog.Builder(RequireContext());
        builder.SetTitle(GetString(Resource.String.date_picker_label));
        builder.SetView(datePicker);
        builder.SetPositiveButton("OK", (_, _) =>
        {
            OnDateSelected(datePicker.Year, datePicker.Month, datePicker.DayOfMonth);
        });
        builder.SetNegativeButton("Cancelar", (_, _) => { });
        builder.Show();
    }

    private void ShowDatePickerWithClearOption()
    {
        var items = new List<string>
        {
            GetString(Resource.String.date_picker_clear)!,
            GetString(Resource.String.date_picker_label)!
        };

        var builder = new AlertDialog.Builder(RequireContext());
        builder.SetTitle(string.Format(GetString(Resource.String.date_picker_selected)!,
            _selectedDate!.Value.ToString("dd/MM/yyyy")));
        builder.SetItems(items.ToArray(), (_, args) =>
        {
            if (args.Which == 0)
            {
                _selectedDate = null;
                _showLiveOnly = false;
                if (_liveChip != null)
                    _liveChip.Checked = false;
                UpdateDateButtonText();
                ApplyFilter();
            }
            else
            {
                ShowDatePicker();
            }
        });
        builder.Show();
    }

    private void OnDateSelected(int year, int month, int dayOfMonth)
    {
        var selected = new DateTime(year, month + 1, dayOfMonth);

        if (selected.Year != CopaYear ||
            selected.Month < CopaMonthStart ||
            selected.Month > CopaMonthEnd)
            return;

        _selectedDate = selected;

        if (_showLiveOnly)
        {
            _showLiveOnly = false;
            if (_liveChip != null)
                _liveChip.Checked = false;
        }

        UpdateDateButtonText();
        ApplyFilter();
    }

    private void UpdateDateButtonText()
    {
        if (_datePickerButton == null)
            return;

        if (_selectedDate.HasValue)
        {
            _datePickerButton.Text = string.Format(
                GetString(Resource.String.date_picker_selected)!,
                _selectedDate.Value.ToString("dd/MM/yyyy"));
        }
        else
        {
            _datePickerButton.Text = GetString(Resource.String.date_picker_label);
        }
    }

    // ────────── Aplicar filtro ──────────

    private void ApplyFilter()
    {
        IEnumerable<Game> filtered = _knockoutGames;

        if (_showLiveOnly)
        {
            filtered = filtered.Where(g => g.IsLive);
        }
        else if (_selectedDate.HasValue)
        {
            var targetKey = _selectedDate.Value.ToString("dd/MM");
            filtered = filtered.Where(g =>
            {
                var dateKey = GameDisplayHelper.FormatShortDate(g.LocalDate, g.StadiumId);
                return dateKey == targetKey;
            });
        }

        var items = new List<GameListItem>();

        if (_showLiveOnly)
        {
            foreach (var game in filtered.OrderBy(g => g.LocalDate))
                items.Add(new GameListItem { IsHeader = false, Game = game });
        }
        else if (_selectedDate.HasValue)
        {
            foreach (var stageGroup in filtered
                .GroupBy(g => g.Stage)
                .OrderBy(g => g.Key.SortOrder()))
            {
                items.Add(new GameListItem
                {
                    IsHeader = true,
                    HeaderText = stageGroup.Key.ToDisplayName()
                });
                foreach (var game in stageGroup.OrderBy(g => g.LocalDate))
                    items.Add(new GameListItem { IsHeader = false, Game = game });
            }
        }
        else
        {
            foreach (var stageGroup in filtered
                .GroupBy(g => g.Stage)
                .OrderBy(g => g.Key.SortOrder()))
            {
                items.Add(new GameListItem
                {
                    IsHeader = true,
                    HeaderText = stageGroup.Key.ToDisplayName()
                });
                foreach (var game in stageGroup.OrderBy(g => g.LocalDate))
                    items.Add(new GameListItem { IsHeader = false, Game = game });
            }
        }

        if (_adapter != null)
            _adapter.SetItems(items);
    }

    // ────────── Helpers de UI ──────────

    private void ShowLoading(bool show)
    {
        if (_progressBar != null)
            _progressBar.Visibility = show ? ViewStates.Visible : ViewStates.Gone;
        if (show && _errorLayout != null)
            _errorLayout.Visibility = ViewStates.Gone;
    }

    private void ShowError(bool show)
    {
        if (_errorLayout != null)
            _errorLayout.Visibility = show ? ViewStates.Visible : ViewStates.Gone;
        if (_recyclerView != null)
            _recyclerView.Visibility = show ? ViewStates.Gone : ViewStates.Visible;
    }

    private void RunOnUi(Action action) =>
        UiHelper.RunOnUiThreadSafe(Activity, action);
}
