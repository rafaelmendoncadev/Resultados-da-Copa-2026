using Android.OS;
using Android.Views;
using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;
using AndroidX.SwipeRefreshLayout.Widget;
using Google.Android.Material.Chip;
using Resultados_da_Copa_2026.Adapters;
using Resultados_da_Copa_2026.Models;
using Resultados_da_Copa_2026.Services;

namespace Resultados_da_Copa_2026.Fragments;

public class KnockoutFragment : AndroidX.Fragment.App.Fragment
{
    private MatchRepository? _repository;
    private GameListAdapter? _adapter;
    private CalendarDateAdapter? _calendarAdapter;
    private SwipeRefreshLayout? _swipeRefresh;
    private RecyclerView? _recyclerView;
    private RecyclerView? _calendarRecycler;
    private ProgressBar? _progressBar;
    private View? _errorLayout;
    private Chip? _liveChip;
    private Dictionary<string, string>? _stadiumCities;
    private List<Game> _knockoutGames = [];
    private string? _selectedDate;   // "dd/MM" ou null (todas)
    private bool _showLiveOnly;

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

    // Dias da semana em português (abreviado)
    private static readonly string[] DiaSemana = ["Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb"];

    // Meses abreviados em português
    private static readonly string[] Meses = ["", "Jan", "Fev", "Mar", "Abr", "Mai", "Jun",
        "Jul", "Ago", "Set", "Out", "Nov", "Dez"];

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
        _calendarRecycler = view.FindViewById<RecyclerView>(Resource.Id.calendarRecycler);
        _progressBar = view.FindViewById<ProgressBar>(Resource.Id.progressBar);
        _errorLayout = view.FindViewById(Resource.Id.errorLayout);
        _liveChip = view.FindViewById<Chip>(Resource.Id.liveChip);

        // Adapter da lista de jogos
        _adapter = new GameListAdapter();
        _adapter.ItemClick += game =>
        {
            var intent = new Android.Content.Intent(RequireContext(), typeof(Activities.GameDetailActivity));
            intent.PutExtra(Activities.GameDetailActivity.ExtraGameId, game.Id);
            StartActivity(intent);
        };
        _recyclerView!.SetLayoutManager(new LinearLayoutManager(RequireContext()));
        _recyclerView.SetAdapter(_adapter);

        // Adapter do calendário horizontal
        _calendarAdapter = new CalendarDateAdapter(RequireContext());
        _calendarAdapter.DateClick += OnCalendarDateClick;
        _calendarRecycler!.SetLayoutManager(
            new LinearLayoutManager(RequireContext(), LinearLayoutManager.Horizontal, false));
        _calendarRecycler.SetAdapter(_calendarAdapter);

        // Chip Ao Vivo
        if (_liveChip != null)
        {
            _liveChip.Checked = _showLiveOnly;
            _liveChip.Click += (_, _) =>
            {
                _showLiveOnly = _liveChip!.Checked;
                if (_showLiveOnly)
                {
                    _selectedDate = null;
                    _calendarAdapter!.UpdateSelection(null);
                }
                ApplyFilter();
            };
        }

        _swipeRefresh!.SetColorSchemeResources(Resource.Color.wc_green_dark);
        _swipeRefresh.Refresh += async (_, _) => await LoadDataAsync(forceRefresh: true);

        view.FindViewById<Google.Android.Material.Button.MaterialButton>(Resource.Id.retryButton)!
            .Click += async (_, _) => await LoadDataAsync(forceRefresh: true);

        _ = LoadDataAsync();
    }

    // ────────── Carregamento de dados ──────────

    private async Task LoadDataAsync(bool forceRefresh = false)
    {
        ShowLoading(true);
        try
        {
            var result = await _repository!.GetGamesAsync(RequireContext(), forceRefresh);

            // Carrega estádios para conversão de fuso horário
            _stadiumCities = (await _repository.GetStadiumsAsync(RequireContext(), forceRefresh)).Data
                .Where(s => s.CityEn != null)
                .ToDictionary(s => s.Id, s => s.CityEn!);

            if (_adapter != null)
                _adapter.StadiumCities = _stadiumCities;

            _knockoutGames = result.Data
                .Where(g => KnockoutStages.Contains(g.Stage))
                .OrderBy(g => g.Stage.SortOrder())
                .ThenBy(g => int.TryParse(g.Id, out var id) ? id : 999)
                .ToList();

            SetupCalendar();
            ApplyFilter();

            DataLoaded?.Invoke(new DataResult<List<Game>>
            {
                Data = _knockoutGames,
                FromCache = result.FromCache,
                CachedAt = result.CachedAt,
                IsOffline = result.IsOffline
            });
            ShowError(_knockoutGames.Count == 0);
        }
        catch
        {
            ShowError(true);
        }
        finally
        {
            ShowLoading(false);
            _swipeRefresh!.Refreshing = false;
        }
    }

    // ────────── Calendário horizontal ──────────

    private void SetupCalendar()
    {
        if (_calendarAdapter == null)
            return;

        // Agrupa jogos por data (dd/MM em Brasília)
        var dateGroups = _knockoutGames
            .GroupBy(g => GameDisplayHelper.FormatShortDate(g.LocalDate, g.StadiumId, _stadiumCities))
            .OrderBy(g => g.Key)
            .ToList();

        var items = new List<CalendarDateItem>();

        // Item "Todas" (sem filtro)
        items.Add(new CalendarDateItem
        {
            DateKey = null!,  // todas
            DayName = null,
            DayNumber = GetString(Resource.String.calendar_all)!,
            Month = "",
            HasGames = true,
            IsSelected = _selectedDate == null && !_showLiveOnly
        });

        foreach (var group in dateGroups)
        {
            if (DateTime.TryParseExact(group.Key, "dd/MM",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dt))
            {
                items.Add(new CalendarDateItem
                {
                    DateKey = group.Key,
                    DayName = DiaSemana[(int)dt.DayOfWeek],
                    DayNumber = dt.Day.ToString(),
                    Month = Meses[dt.Month],
                    HasGames = true,
                    IsSelected = group.Key == _selectedDate
                });
            }
            else
            {
                // Fallback: mostra a chave textual
                items.Add(new CalendarDateItem
                {
                    DateKey = group.Key,
                    DayName = null,
                    DayNumber = group.Key,
                    Month = "",
                    HasGames = true,
                    IsSelected = group.Key == _selectedDate
                });
            }
        }

        _calendarAdapter.SetDates(items);
    }

    private void OnCalendarDateClick(CalendarDateItem item)
    {
        if (_showLiveOnly)
        {
            _showLiveOnly = false;
            if (_liveChip != null)
                _liveChip.Checked = false;
        }

        _selectedDate = string.IsNullOrEmpty(item.DateKey) ? null : item.DateKey;
        _calendarAdapter?.UpdateSelection(_selectedDate);
        ApplyFilter();
    }

    // ────────── Aplicar filtro ──────────

    private void ApplyFilter()
    {
        IEnumerable<Game> filtered = _knockoutGames;

        if (_showLiveOnly)
        {
            // Mostra apenas jogos AO VIVO
            filtered = filtered.Where(g => g.IsLive);
        }
        else if (_selectedDate != null)
        {
            // Filtra por data (dd/MM em Brasília)
            filtered = filtered.Where(g =>
            {
                var dateKey = GameDisplayHelper.FormatShortDate(g.LocalDate, g.StadiumId, _stadiumCities);
                return dateKey == _selectedDate;
            });
        }

        // Monta itens: agrupados por fase quando mostra todas, sem agrupar quando filtrado
        var items = new List<GameListItem>();

        if (_showLiveOnly)
        {
            foreach (var game in filtered.OrderBy(g => g.LocalDate))
                items.Add(new GameListItem { IsHeader = false, Game = game });
        }
        else if (_selectedDate != null)
        {
            // Filtrado por data: agrupa por fase
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
            // Todas: agrupa por fase (comportamento original)
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

        _adapter!.SetItems(items);
    }

    // ────────── Helpers de UI ──────────

    private void ShowLoading(bool show)
    {
        _progressBar!.Visibility = show ? ViewStates.Visible : ViewStates.Gone;
        if (show)
            _errorLayout!.Visibility = ViewStates.Gone;
    }

    private void ShowError(bool show)
    {
        _errorLayout!.Visibility = show ? ViewStates.Visible : ViewStates.Gone;
        _recyclerView!.Visibility = show ? ViewStates.Gone : ViewStates.Visible;
    }
}
