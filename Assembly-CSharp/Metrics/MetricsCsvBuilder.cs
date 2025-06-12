using System.Text;

namespace Metrics;

public readonly struct MetricsCsvBuilder
{
	public readonly string Filename;

	public readonly StringBuilder StrBuilder;

	public static string TimestampNow => TimeBehaviour.FormatTime("yyyy-MM-dd HH.mm.ss");

	public MetricsCsvBuilder(MetricsCollectorBase source, string filename = null)
	{
		if (filename == null)
		{
			filename = "Export {0}";
		}
		if (source != null)
		{
			filename = source.GetType().Name.Replace("Collector", string.Empty) + "/" + filename;
		}
		this.Filename = string.Format(filename.TrimEnd('.') + ".csv", MetricsCsvBuilder.TimestampNow);
		this.StrBuilder = new StringBuilder();
	}

	public void Append(string str)
	{
		this.StrBuilder.Append(str);
	}

	public void Append(object obj)
	{
		this.StrBuilder.Append(obj);
	}

	public void Append(char ch)
	{
		this.StrBuilder.Append(ch);
	}

	public void AppendLine()
	{
		this.StrBuilder.AppendLine();
	}

	public void AppendColumn(string str)
	{
		this.StrBuilder.Append(str);
		this.StrBuilder.Append(',');
	}

	public void AppendColumn(object obj)
	{
		this.StrBuilder.Append(obj);
		this.StrBuilder.Append(',');
	}

	public MetricsCsvBuilder[] ToArray()
	{
		return new MetricsCsvBuilder[1] { this };
	}
}
