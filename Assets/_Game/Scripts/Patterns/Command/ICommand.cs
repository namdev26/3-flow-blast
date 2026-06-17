namespace FlowBlast.Patterns.Command
{
    public interface ICommand
    {
        bool CanExecute();
        void Execute();
    }
}
