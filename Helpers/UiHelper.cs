using Android.App;

namespace Resultados_da_Copa_2026.Helpers;

public static class UiHelper
{
    public static void RunOnUiThreadSafe(Activity? activity, Action action)
    {
        if (activity == null || activity.IsFinishing)
            return;

        if (activity.IsDestroyed)
            return;

        activity.RunOnUiThread(action);
    }
}
