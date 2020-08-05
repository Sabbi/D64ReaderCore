namespace D64Reader.Renderers
{
    public interface ID64Renderer<T>
    {
        T Render(D64Directory directory);
    }
}