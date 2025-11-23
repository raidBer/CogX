using Microsoft.AspNetCore.SignalR;

namespace CogX.Hubs
{
    public class LobbyHub : Hub
    {
        // Rejoindre un groupe SignalR pour recevoir les mises à jour d'un lobby
        public async Task JoinLobbyGroup(string lobbyId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);
        }

        // Quitter un groupe SignalR
        public async Task LeaveLobbyGroup(string lobbyId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, lobbyId);
        }

        // S'abonner à la liste de tous les lobbies publics
        public async Task SubscribeToLobbyList()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "LobbyList");
        }

        // Se désabonner de la liste des lobbies
        public async Task UnsubscribeFromLobbyList()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "LobbyList");
        }

        // Gérer la déconnexion
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Ici on pourrait gérer le retrait automatique du joueur du lobby
            // Pour l'instant on laisse simple
            await base.OnDisconnectedAsync(exception);
        }
    }
}
