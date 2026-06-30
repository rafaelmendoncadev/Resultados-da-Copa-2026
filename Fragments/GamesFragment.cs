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

public class GamesFragment : AndroidX.Fragment.App.Fragment
{
    private MatchRepository? _repository;
    private GameListAdapter? _adapter;
    private SwipeRefreshLayout? _swipeRefresh;
    private RecyclerView? _recyclerView;
    private ProgressBar? _progressBar;
    private View? _errorLayout;
    private ChipGroup? _groupChipGroup;
    private ChipGroup? _dateChipGroup;
    private List<Game> _allGames = [];
    private Dictionary<string, string>? _stadiumCities;
    private string? _selectedGroup;
    private string? _selectedDate;     // "dd/MM" ou null (todas) ou "__live__"

    private const string LiveFilterKey = "__live__";

    public event Action<DataResult<List<Game>>>? DataLoaded;

    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        return inflater.Inflate(Resource.Layout.fragment_games, container, false);
    }

    public override void OnViewCreated(View view, Bundle? savedInstanceState)
    {
        base.OnViewCreated(view, savedInstanceState);
        _repository = new MatchRepository(RequireContext());
        _swipeRefresh = view.FindViewById<SwipeRefreshLayout>(Resource.Id.swipeRefresh);
        _recyclerView = view.FindViewById<RecyclerView>(Resource.Id.recyclerView);
        _progressBar = view.FindViewById<ProgressBar>(Resource.Id.progressBar);
        _errorLayout = view.FindViewById(Resource.Id.errorLayout);
        _groupChipGroup = view.FindViewById<ChipGroup>(Resource.Id.groupChipGroup);
        _dateChipGroup = view.FindViewById<ChipGroup>(Resource.Id.dateChipGroup);

        _adapter = new GameListAdapter();
        _adapter.ItemClick += game =>
        {
            var intent = new Android.Content.Intent(RequireContext(), typeof(Activities.GameDetailActivity));
            intent.PutExtra(Activities.GameDetailActivity.ExtraGameId, game.Id);
            StartActivity(intent);
        };

        _recyclerView!.SetLayoutManager(new LinearLayoutManager(RequireContext()));
        _recyclerView.SetAdapter(_adapter);

        _swipeRefresh!.SetColorSchemeResources(Resource.Color.wc_green_dark);
        _swipeRefresh.Refresh += async (_, _) => await LoadDataAsync(forceRefresh: true);

        view.FindViewById<Google.Android.Material.Button.MaterialButton>(Resource.Id.retryButton)!
            .Click += async (_, _) => await LoadDataAsync(forceRefresh: true);

        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync(bool forceRefresh = false)
    {
        ShowLoading(true);
        try
        {
            var result = await _repository!.GetGamesAsync(RequireContext(), forceRefresh);
            _allGames = result.Data.Where(g => g.Stage == MatchStage.Group).ToList();

            // Carrega estádios para conversão de fuso horário
            _stadiumCities = (await _repository.GetStadiumsAsync(RequireContext(), forceRefresh)).Data
                .Where(s => s.CityEn != null)
                .ToDictionary(s => s.Id, s => s.CityEn!);

            if (_adapter != null)
                _adapter.StadiumCities = _stadiumCities;

            DataLoaded?.Invoke(result);
            SetupGroupChips();
            SetupDateChips();
            ApplyFilter();
            ShowError(_allGames.Count == 0);
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

    // ────────── Filtro por GRUPO ──────────

    private void SetupGroupChips()
    {
        if (_groupChipGroup == null)
            return;

        _groupChipGroup.RemoveAllViews();
        AddGroupChip(null, GetString(Resource.String.filter_all)!);

        foreach (var group in _allGames.Select(g => g.Group).Distinct().OrderBy(g => g))
            AddGroupChip(group, $"Grupo {group}");
    }

    private void AddGroupChip(string? group, string label)
    {
        var chip = new Chip(RequireContext())
        {
            Text = label,
            Checkable = true,
            Checked = group == _selectedGroup || (group == null && _selectedGroup == null)
        };
        chip.SetChipBackgroundColorResource(Resource.Color.wc_background);
        chip.Click += (_, _) =>
        {
            _selectedGroup = group;
            ApplyFilter();
        };
        _groupChipGroup!.AddView(chip);
    }

    // ────────── Filtro por DATA / AO VIVO ──────────

    private void SetupDateChips()
    {
        if (_dateChipGroup == null)
            return;

        _dateChipGroup.RemoveAllViews();

        // Chip "Ao Vivo"
        AddDateChip(LiveFilterKey, GetString(Resource.String.filter_live)!);

        // Chip "Todas as datas"
        AddDateChip(null, GetString(Resource.String.filter_date_all)!);

        // Chip para cada data distinta
        foreach (var dateKey in ObterDatasDistintas())
            AddDateChip(dateKey, dateKey);
    }

    private List<string> ObterDatasDistintas()
    {
        return _allGames
            .Select(g => GameDisplayHelper.FormatShortDate(g.LocalDate, g.StadiumId, _stadiumCities))
            .Distinct()
            .OrderBy(d => d)
            .ToList();
    }

    private void AddDateChip(string? key, string label)
    {
        var chip = new Chip(RequireContext())
        {
            Text = label,
            Checkable = true,
            Checked = key == _selectedDate || (key == null && _selectedDate == null)
        };
        chip.SetChipBackgroundColorResource(Resource.Color.wc_background);
        chip.Click += (_, _) =>
        {
            _selectedDate = key;
            ApplyFilter();
        };
        _dateChipGroup!.AddView(chip);
    }

    // ────────── Aplicar filtros combinados ──────────

    private void ApplyFilter()
    {
        IEnumerable<Game> filtered = _allGames;

        // Filtro de grupo
        if (!string.IsNullOrEmpty(_selectedGroup))
            filtered = filtered.Where(g => g.Group == _selectedGroup);

        // Filtro de data / ao vivo
        if (_selectedDate == LiveFilterKey)
        {
            // Mostra apenas jogos AO VIVO
            filtered = filtered.Where(g => g.IsLive);
        }
        else if (_selectedDate != null)
        {
            // Filtra pela data (dd/MM em Brasília)
            filtered = filtered.Where(g =>
            {
                var dateKey = GameDisplayHelper.FormatShortDate(g.LocalDate, g.StadiumId, _stadiumCities);
                return dateKey == _selectedDate;
            });
        }

        // Monta os itens agrupados
        var items = new List<GameListItem>();

        if (_selectedDate == LiveFilterKey)
        {
            // Ao vivo: sem cabeçalho de grupo
            foreach (var game in filtered.OrderBy(g => g.LocalDate))
                items.Add(new GameListItem { IsHeader = false, Game = game });
        }
        else
        {
            // Agrupa por grupo
            foreach (var group in filtered.GroupBy(g => g.Group).OrderBy(g => g.Key))
            {
                items.Add(new GameListItem { IsHeader = true, HeaderText = $"Grupo {group.Key}" });
                foreach (var game in group.OrderBy(g => g.LocalDate))
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
