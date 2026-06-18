using System.Diagnostics.CodeAnalysis;
using SQLite;

namespace EduDev_Tracker.Data.Repositories.Implementations
{
    // [DynamicallyAccessedMembers] сообщает IL Trimmer: "для любого T сохрани все публичные свойства".
    // Без этого линкер вырезает геттеры/сеттеры моделей, к которым SQLite-net обращается через рефлексию.
    public abstract class BaseRepository<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
        where T : new()
    {
        protected readonly DatabaseService Db;
        protected SQLiteAsyncConnection Connection => Db.Connection;

        protected BaseRepository(DatabaseService db) => Db = db;

        public virtual Task<List<T>> GetAllAsync() => Connection.Table<T>().ToListAsync();

        public virtual Task<T?> GetByIdAsync(int id) => Connection.FindAsync<T>(id)!;

        public virtual async Task<int> SaveAsync(T entity)
        {
            var idProp = typeof(T).GetProperty("Id");
            var id = (int)(idProp?.GetValue(entity) ?? 0);
            return id == 0 ? await Connection.InsertAsync(entity) : await Connection.UpdateAsync(entity);
        }

        public virtual Task<int> DeleteAsync(T entity) => Connection.DeleteAsync(entity);

        public Task<int> DeleteByIdAsync(int id) => Connection.DeleteAsync<T>(id);
    }
}
