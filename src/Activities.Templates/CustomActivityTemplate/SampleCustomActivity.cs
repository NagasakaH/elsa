using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace NagasakaEventSystem.Activities.Templates.CustomActivityTemplate;

[Activity("Custom", "Sample", "サンプルのカスタムアクティビティ")]
public sealed class SampleCustomActivity : CodeActivity
{
    [Input(Description = "入力テキスト")]
    public Input<string> Text { get; set; } = default!;

    [Output(Description = "入力を加工した結果")]
    public Output<string> Result { get; set; } = default!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var text = context.Get(Text);
        context.Set(Result, $"Echo:{text}");
        return context.CompleteActivityAsync();
    }
}
