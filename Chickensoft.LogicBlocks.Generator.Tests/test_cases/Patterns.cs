namespace Chickensoft.LogicBlocks.Generator.Tests;

[StateMachine]
public class Patterns : LogicBlock<Patterns.Input, Patterns.State, LightSwitch.Output> {
  public enum Mode { One, Two, Three }

  public override State GetInitialState(IContext context) =>
    new State.One(context);

  public abstract record Input {
    public record SetMode(Mode Mode) : Input;
  }

  public abstract record State(IContext Context) : StateLogic(Context), IGet<Input.SetMode> {
    public State On(Input.SetMode input) => input.Mode switch {
      Mode.One => new One(Context),
      Mode.Two => new Two(Context),
      Mode.Three => true switch {
        true => new Three(Context),
        false => throw new NotImplementedException()
      },
      _ => throw new NotImplementedException()
    };
    public record One(IContext Context) : State(Context);
    public record Two(IContext Context) : State(Context);
    public record Three(IContext Context) : State(Context);
  }

  public abstract record Output { }
}