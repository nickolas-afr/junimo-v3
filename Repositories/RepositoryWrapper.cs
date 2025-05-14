using junimo_v3.Data;
using junimo_v3.Repositories.Interfaces;

namespace junimo_v3.Repositories
{
    public class RepositoryWrapper : IRepositoryWrapper
    {
        private readonly RepositoryContext _repositoryContext;
        private IGameRepository _gameRepository;
        private IGenreRepository _genreRepository;
        private IUserRepository _userRepository;
        private IReviewRepository _reviewRepository;
        private IGameGenreRepository _gameGenreRepository;
        private IOrderItemsRepository _orderItemsRepository;
        private IOrderRepository _orderRepository;
        private ITicketRepository _ticketRepository;
        private IGameGenreV2Repository _gameGenreV2Repository;
        private IUserFriendshipRepository _userFriendshipRepository;

        public RepositoryWrapper(RepositoryContext repositoryContext)
        {
            _repositoryContext = repositoryContext;
        }

        public IGameRepository Game => _gameRepository ??= new GameRepository(_repositoryContext);
        public IGenreRepository Genre => _genreRepository ??= new GenreRepository(_repositoryContext);
        public IUserRepository User => _userRepository ??= new UserRepository(_repositoryContext);
        public IReviewRepository Review => _reviewRepository ??= new ReviewRepository(_repositoryContext);
        public IGameGenreRepository GameGenre => _gameGenreRepository ??= new GameGenreRepository(_repositoryContext);
        public IOrderItemsRepository OrderItems => _orderItemsRepository ??= new OrderItemsRepository(_repositoryContext);
        public IOrderRepository Order => _orderRepository ??= new OrderRepository(_repositoryContext);
        public ITicketRepository Ticket => _ticketRepository ??= new TicketRepository(_repositoryContext);
        public IGameGenreV2Repository GameGenreV2 => _gameGenreV2Repository ??= new GameGenreV2Repository(_repositoryContext);
        public IUserFriendshipRepository UserFriendship => _userFriendshipRepository ??= new UserFriendshipRepository(_repositoryContext);

        public async Task SaveAsync()
        {
            await _repositoryContext.SaveChangesAsync();
        }
    }
}
