using Android.OS;
using Android.Views;
using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;
using AndroidX.SwipeRefreshLayout.Widget;
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
    private Dictionary<string, string>? _stadiumCities;

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

            // Carrega estádios para conversão de fuso horário
            _stadiumCities = (await _repository.GetStadiumsAsync(RequireContext(), forceRefresh)).Data
                .Where(s => s.CityEn != null)
                .ToDictionary(s => s.Id, s => s.CityEn!);

            if (_adapter != null)
                _adapter.StadiumCities = _stadiumCities;

            var knockoutGames = result.Data
                .Where(g => KnockoutStages.Contains(g.Stage))
                .OrderBy(g => g.Stage.SortOrder())
                .ThenBy(g => int.TryParse(g.Id, out var id) ? id : 999)
                .ToList();

            var items = new List<GameListItem>();
            foreach (var stageGroup in knockoutGames.GroupBy(g => g.Stage).OrderBy(g => g.Key.SortOrder()))
            {
                items.Add(new GameListItem
                {
                    IsHeader = true,
                    HeaderText = stageGroup.Key.ToDisplayName()
                });

                foreach (var game in stageGroup)
                    items.Add(new GameListItem { IsHeader = false, Game = game });
            }

            _adapter!.SetItems(items);
            DataLoaded?.Invoke(new DataResult<List<Game>>
            {
                Data = knockoutGames,
                FromCache = result.FromCache,
                CachedAt = result.CachedAt,
                IsOffline = result.IsOffline
            });
            ShowError(knockoutGames.Count == 0);
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
