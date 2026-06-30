using System.Text.Json.Serialization;

namespace Resultados_da_Copa_2026.Services;

[JsonSerializable(typeof(Models.Game))]
[JsonSerializable(typeof(Models.GamesResponse))]
[JsonSerializable(typeof(Models.GroupsResponse))]
[JsonSerializable(typeof(Models.TeamsResponse))]
[JsonSerializable(typeof(Models.StadiumsResponse))]
[JsonSerializable(typeof(Models.OpenFootballRoot))]
[JsonSerializable(typeof(List<Models.Game>))]
[JsonSerializable(typeof(List<Models.GroupStanding>))]
[JsonSerializable(typeof(List<Models.Team>))]
[JsonSerializable(typeof(List<Models.Stadium>))]
internal partial class AppJsonContext : JsonSerializerContext;
