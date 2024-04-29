namespace Chickensoft.LogicBlocks.Tests.Fixtures;

using Chickensoft.Introspection;

[Introspective("greedy_logic")]
[LogicBlock(typeof(State))]
public partial class GreedyLogic : LogicBlock<GreedyLogic.State> {
  public override Transition GetInitialState() => To<State.A>();

  public static class Input {
    public readonly record struct GoToB;
    public readonly record struct GoToC;
  }

  [Introspective("greedy_logic_state")]
  public abstract partial record State : StateLogic<State> {
    [Introspective("greedy_logic_state_a")]
    public partial record A : State, IGet<Input.GoToB>, IGet<Input.GoToC> {
      public A() {
        OnAttach(() => {
          Input(new Input.GoToB());
          Input(new Input.GoToC());
        });
      }

      public Transition On(Input.GoToB input) => To<B>();
      public Transition On(Input.GoToC input) => To<C>();
    }

    [Introspective("greedy_logic_state_b")]
    public partial record B : State { }
    [Introspective("greedy_logic_state_c")]
    public partial record C : State { }
  }
}
