using Android.Content;
using Android.Net;

namespace Resultados_da_Copa_2026.Services;

public static class NetworkHelper
{
    public static bool IsNetworkAvailable(Context context)
    {
        var connectivityManager = (ConnectivityManager?)context.GetSystemService(Context.ConnectivityService);
        if (connectivityManager == null)
            return false;

        var network = connectivityManager.ActiveNetwork;
        if (network == null)
            return false;

        var capabilities = connectivityManager.GetNetworkCapabilities(network);
        return capabilities != null &&
               (capabilities.HasTransport(TransportType.Wifi) ||
                capabilities.HasTransport(TransportType.Cellular) ||
                capabilities.HasTransport(TransportType.Ethernet));
    }
}
