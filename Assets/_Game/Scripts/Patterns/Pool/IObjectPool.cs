namespace FlowBlast.Patterns.Pool
{
    public interface IObjectPool<T>
    {
        T Get();
        void Release(T item);
        void Prewarm(int count);
    }
}
