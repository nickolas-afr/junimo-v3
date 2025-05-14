namespace junimo_v3.Repositories.Interfaces
{
    public interface IRepositoryWrapper
    {
        IGameRepository Game { get; }
        IGenreRepository Genre { get; }
        IUserRepository User { get; }
        IReviewRepository Review { get; }
        IGameGenreRepository GameGenre { get; }
        IGameGenreV2Repository GameGenreV2 { get; }
        IOrderItemsRepository OrderItems { get; }
        IOrderRepository Order { get; }
        ITicketRepository Ticket { get; }
        IUserFriendshipRepository UserFriendship { get; }
        Task SaveAsync();
    }
}
