namespace Birdie.Birds
{
    /// <summary>
    /// Implemented by any GameObject that can receive a click event
    /// routed from <see cref="TransparentGame"/> via sprite hit-testing.
    /// </summary>
    public interface IClickable
    {
        void OnClicked();
    }
}
