namespace FlowBlast.Patterns.Pool
{
    public interface IObjectPool<T>
    {
        bool TryGet(out T item);
        void Release(T item);
        void Consume(T item);
        void Prewarm(int count);
        void RecycleAll();
    }
}
