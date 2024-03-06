namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.LogicBlocks.Generator;

public interface IMyLogicBlock : ILogicBlock<MyLogicBlock.State> { }

[StateDiagram(typeof(State))]
public partial class MyLogicBlock : LogicBlock<MyLogicBlock.State>, IMyLogicBlock {
  public override State GetInitialState() => new State.SomeState();

  public static class Input {
    public readonly record struct SomeInput;
    public readonly record struct SomeOtherInput;
  }

  public abstract record State : StateLogic<State> {
    public record SomeState : State, IGet<Input.SomeInput> {
      public SomeState() {
        this.OnEnter(() => Context.Output(new Output.SomeOutput()));
        this.OnExit(() => Context.Output(new Output.SomeOutput()));
      }

      public State On(Input.SomeInput input) {
        Context.Output(new Output.SomeOutput());
        return new SomeOtherState();
      }
    }

    public record SomeOtherState : State,
      IGet<Input.SomeOtherInput> {
      public State On(Input.SomeOtherInput input) {
        Context.Output(new Output.SomeOtherOutput());
        return new SomeState();
      }
    }
  }

  public static class Output {
    public readonly record struct SomeOutput;
    public readonly record struct SomeOtherOutput;
  }
}
