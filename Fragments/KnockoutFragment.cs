using Android.App;
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
    private SwipeRefreshLayout? _swipeRefresh;
    private RecyclerView? _recyclerView;
    private ProgressBar? _progressBar;
    private View? _errorLayout;
    private Chip? _liveChip;
    private global::Google.Android.Material.Button.MaterialButton? _datePickerButton;
    private Dictionary<string, string>? _stadiumCities;
    private List<Game> _knockoutGames = [];
    private DateTime? _selectedDate;   // data selecionada no calendário (em Brasília)
    private bool _showLiveOnly;

    // Ano da Copa 2026
    private const int CopaYear = 2026;
    private const int CopaMonthStart = 6;  // Junho
    private const int CopaMonthEnd = 7;    // Julho

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

        // Botão do calendário
        if (_datePickerButton != null)
        {
            UpdateDateButtonText();
            _datePickerButton.Click += OnDatePickerButtonClick;
        }

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

    // ────────── DatePickerDialog ──────────

    // ────────── DatePicker via AlertDialog ──────────

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
            ? now.Month - 1  // DatePicker usa 0-based para mês
            : CopaMonthStart - 1;
        var initialDay = now.Year == CopaYear && (now.Month - 1) == initialMonth
            ? Math.Clamp(now.Day, 1, 30)
            : 11;

        var datePicker = new DatePicker(RequireContext());
        datePicker.UpdateDate(initialYear, initialMonth, initialDay);

        // Limita o calendário a Junho e Julho de 2026
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
        // month do DatePicker é 0-based (Janeiro = 0)
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
            // Filtra por data (dd/MM em Brasília)
            var targetKey = _selectedDate.Value.ToString("dd/MM");
            filtered = filtered.Where(g =>
            {
                var dateKey = GameDisplayHelper.FormatShortDate(g.LocalDate, g.StadiumId, _stadiumCities);
                return dateKey == targetKey;
            });
        }

        // Monta itens
        var items = new List<GameListItem>();

        if (_showLiveOnly)
        {
            foreach (var game in filtered.OrderBy(g => g.LocalDate))
                items.Add(new GameListItem { IsHeader = false, Game = game });
        }
        else if (_selectedDate.HasValue)
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
}
