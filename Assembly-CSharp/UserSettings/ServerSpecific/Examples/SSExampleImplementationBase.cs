namespace UserSettings.ServerSpecific.Examples;

public abstract class SSExampleImplementationBase
{
	private static SSExampleImplementationBase _activeExample;

	public static readonly SSExampleImplementationBase[] AllExamples = new SSExampleImplementationBase[6]
	{
		new SSFieldsDemoExample(),
		new SSAbilitiesExample(),
		new SSTextAreaExample(),
		new SSPagesExample(),
		new SSPrimitiveSpawnerExample(),
		new SSLightSpawnerExample()
	};

	public abstract string Name { get; }

	public abstract void Activate();

	public abstract void Deactivate();

	public static bool TryActivateExample(int index, out string message)
	{
		if (!SSExampleImplementationBase.AllExamples.TryGet(index, out var element))
		{
			message = $"Index {index} out of range.";
			return false;
		}
		if (SSExampleImplementationBase._activeExample == element)
		{
			message = "This example is already active. Use the 'stop' argument to deactivate.";
			return false;
		}
		if (SSExampleImplementationBase._activeExample != null)
		{
			message = "Another example is already active. Use the 'stop' argument to deactivate.";
			return false;
		}
		SSExampleImplementationBase._activeExample = element;
		SSExampleImplementationBase._activeExample.Activate();
		message = element.Name + " activated.";
		return true;
	}

	public static bool TryDeactivateExample(out string disabledName)
	{
		if (SSExampleImplementationBase._activeExample == null)
		{
			disabledName = null;
			return false;
		}
		disabledName = SSExampleImplementationBase._activeExample.Name;
		SSExampleImplementationBase._activeExample.Deactivate();
		SSExampleImplementationBase._activeExample = null;
		ServerSpecificSettingsSync.DefinedSettings = null;
		ServerSpecificSettingsSync.SendToAll();
		return true;
	}
}
