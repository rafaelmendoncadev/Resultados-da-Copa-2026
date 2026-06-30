using Android.OS;
using AndroidX.AppCompat.App;
using Resultados_da_Copa_2026.Helpers;
using Resultados_da_Copa_2026.Models;
using Resultados_da_Copa_2026.Services;

namespace Resultados_da_Copa_2026.Activities;

[Activity(Label = "@string/app_name", Theme = "@style/AppTheme")]
public class GameDetailActivity : AppCompatActivity
{
    public const string ExtraGameId = "game_id";

    protected override async void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_game_detail);

        var gameId = Intent?.GetStringExtra(ExtraGameId);
        if (string.IsNullOrEmpty(gameId))
        {
            Finish();
            return;
        }

        ActionBar?.SetDisplayHomeAsUpEnabled(true);
        Title = "Detalhe do jogo";

        try
        {
            var repository = new MatchRepository(this);
            var game = await repository.GetGameByIdAsync(this, gameId);
            if (game == null)
            {
                Finish();
                return;
            }

            var stadiums = await repository.GetStadiumsAsync(this);
            var stadiumCities = stadiums.Data
                .Where(s => s.CityEn != null)
                .ToDictionary(s => s.Id, s => s.CityEn!);
            var stadiumName = stadiums.Data.FirstOrDefault(s => s.Id == game.StadiumId)?.NameEn;

            UiHelper.RunOnUiThreadSafe(this, () => BindGame(game, stadiumName, stadiumCities));
        }
        catch
        {
            Finish();
        }
    }

    private void BindGame(Models.Game game, string? stadiumName, Dictionary<string, string> stadiumCities)
    {
        FindViewById<TextView>(Resource.Id.homeTeamText)!.Text = GameDisplayHelper.GetHomeName(game);
        FindViewById<TextView>(Resource.Id.awayTeamText)!.Text = GameDisplayHelper.GetAwayName(game);
        FindViewById<TextView>(Resource.Id.scoreText)!.Text = GameDisplayHelper.FormatScore(game);
        var stageDisplay = game.Stage.ToDisplayName();
        var qualification = GameDisplayHelper.GetQualificationText(game);
        if (!string.IsNullOrEmpty(qualification))
            stageDisplay += $"\n{qualification}";
        FindViewById<TextView>(Resource.Id.stageText)!.Text = stageDisplay;
        FindViewById<TextView>(Resource.Id.statusText)!.Text = GameDisplayHelper.GetStatusText(game);
        FindViewById<TextView>(Resource.Id.dateText)!.Text = GameDisplayHelper.FormatDate(game.LocalDate, game.StadiumId, stadiumCities);
        FindViewById<TextView>(Resource.Id.stadiumText)!.Text = stadiumName ?? $"Estádio #{game.StadiumId}";

        var liveBadge = FindViewById<TextView>(Resource.Id.liveBadge)!;
        liveBadge.Visibility = game.IsLive ? Android.Views.ViewStates.Visible : Android.Views.ViewStates.Gone;

        var homeScorers = GameDisplayHelper.ParseScorers(game.HomeScorers);
        var awayScorers = GameDisplayHelper.ParseScorers(game.AwayScorers);

        var homeScorersView = FindViewById<TextView>(Resource.Id.homeScorersText)!;
        var awayScorersView = FindViewById<TextView>(Resource.Id.awayScorersText)!;

        if (homeScorers.Count == 0 && awayScorers.Count == 0)
        {
            homeScorersView.Text = GetString(Resource.String.no_scorers);
            awayScorersView.Text = string.Empty;
        }
        else
        {
            homeScorersView.Text = homeScorers.Count > 0
                ? $"{GameDisplayHelper.GetHomeName(game)}: {string.Join(", ", homeScorers)}"
                : string.Empty;
            awayScorersView.Text = awayScorers.Count > 0
                ? $"{GameDisplayHelper.GetAwayName(game)}: {string.Join(", ", awayScorers)}"
                : string.Empty;
        }
    }

    public override bool OnOptionsItemSelected(Android.Views.IMenuItem item)
    {
        if (item.ItemId == Android.Resource.Id.Home)
        {
            Finish();
            return true;
        }
        return base.OnOptionsItemSelected(item);
    }
}
