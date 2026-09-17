namespace Netherlands3D.Twin.Functionalities
{
    /// <summary>
    /// Base class for functionalities that store additional,
    /// functionality-specific data in the project
    /// (which can be saved and loaded).
    /// </summary>
    /// <typeparam name="TData">
    /// The concrete <see cref="TypedFunctionalityData"/> type used by this
    /// functionality.
    /// </typeparam>
    /// <example>
    /// <code><![CDATA[
    /// [CreateAssetMenu(menuName = "Netherlands3D/Twin/Functionality/Example", fileName = "Functionality_Example")] 
    /// public class ExampleFunctionality : TypedFunctionality<ExampleFunctionalityData>
    /// {
    /// }
    /// ]]></code>
    /// </example>
    public abstract class TypedFunctionality<TData> : Functionality
        where TData : TypedFunctionalityData<TData>, new()
    {
        public new TData Data
        {
            get => (TData)base.Data;
            set => base.Data = value;
        }

        public new TData CreateDefaultData()
        {
            return (TData)base.CreateDefaultData();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureDefaultDataType<TData>();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            EnsureDefaultDataType<TData>();
        }
    }
}