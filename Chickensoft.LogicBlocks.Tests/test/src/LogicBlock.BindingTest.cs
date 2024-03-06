namespace Chickensoft.LogicBlocks.Tests;

using Chickensoft.LogicBlocks.Tests.Fixtures;
using Chickensoft.LogicBlocks.Tests.TestUtils;
using Moq;
using Shouldly;
using Xunit;

public class BindingTest {
  public static bool WasFinalized { get; set; }

  [Fact]
  public void UpdatesForEveryState() {
    var block = new FakeLogicBlock();
    using var binding = block.Bind();

    var called = 0;
    binding.When<FakeLogicBlock.State>((state) => called++);

    block.Input(new FakeLogicBlock.Input.InputTwo("d", "e"));

    called.ShouldBe(1);
  }

  [Fact]
  public void DoesNotUpdateIfSelectedDataIsSameObject() {
    var block = new FakeLogicBlock();
    using var binding = block.Bind();

    var count = 0;
    binding.When<FakeLogicBlock.State.StateB>(state => count++);

    var a = "a";
    var b = "b";
    block.Input(new FakeLogicBlock.Input.InputTwo(a, b));
    block.Input(new FakeLogicBlock.Input.InputTwo(a, "c"));

    count.ShouldBe(2);
  }

  [Fact]
  public void HandlesEffects() {
    var block = new FakeLogicBlock();
    using var binding = block.Bind();

    var callEffect1 = 0;
    var callEffect2 = 0;

    binding.Handle<FakeLogicBlock.Output.OutputOne>(
      (effect) => { callEffect1++; effect.Value.ShouldBe(1); }
    ).Handle<FakeLogicBlock.Output.OutputTwo>(
      (effect) => { callEffect2++; effect.Value.ShouldBe("2"); }
    );

    // Effects should get handled each time, regardless of if they are
    // identical to the previous one.

    block.Input(new FakeLogicBlock.Input.InputOne(1, 2));
    block.Input(new FakeLogicBlock.Input.InputOne(1, 2));

    block.Input(new FakeLogicBlock.Input.InputTwo("a", "b"));
    block.Input(new FakeLogicBlock.Input.InputTwo("a", "b"));

    callEffect1.ShouldBe(2);
    callEffect2.ShouldBe(2);
  }

  [Fact]
  public void CallsSubstateTransitionsOnlyOnce() {
    var block = new FakeLogicBlock();

    using var binding = block.Bind();

    var callStateA = 0;
    var callStateB = 0;

    binding.When<FakeLogicBlock.State.StateA>((state) => callStateA++);
    binding.When<FakeLogicBlock.State.StateB>((state) => callStateB++);

    callStateA.ShouldBe(0);
    callStateB.ShouldBe(0);
    block.Value.ShouldBe(block.GetInitialState());

    // State is StateA initially, so switch to State B
    block.Input(new FakeLogicBlock.Input.InputTwo("a", "b"));

    callStateA.ShouldBe(0);
    callStateB.ShouldBe(1);
    block.Value.ShouldBeOfType<FakeLogicBlock.State.StateB>();

    block.Input(new FakeLogicBlock.Input.InputTwo("a", "b"));

    callStateA.ShouldBe(0);
    callStateB.ShouldBe(1);
    block.Value.ShouldBeOfType<FakeLogicBlock.State.StateB>();

    block.Input(new FakeLogicBlock.Input.InputTwo("c", "d"));

    callStateA.ShouldBe(0);
    callStateB.ShouldBe(2);
    block.Value.ShouldBeOfType<FakeLogicBlock.State.StateB>();

    block.Input(new FakeLogicBlock.Input.InputOne(1, 2));

    callStateA.ShouldBe(1);
    callStateB.ShouldBe(2);
    block.Value.ShouldBeOfType<FakeLogicBlock.State.StateA>();

    block.Input(new FakeLogicBlock.Input.InputTwo("a", "b"));

    callStateA.ShouldBe(1);
    callStateB.ShouldBe(3);
    block.Value.ShouldBeOfType<FakeLogicBlock.State.StateB>();
  }

  [Fact]
  public void CleansUpSubscriptions() {
    var callStateUpdate = 0;
    var callSideEffectHandler = 0;

    var block = new FakeLogicBlock();
    var binding = block.Bind();

    binding.When<FakeLogicBlock.State.StateA>((value1) => callStateUpdate++);

    binding.Handle<FakeLogicBlock.Output.OutputOne>(
      (effect) => callSideEffectHandler++
    );

    block.Input(new FakeLogicBlock.Input.InputOne(4, 5));

    callStateUpdate.ShouldBe(1);
    callSideEffectHandler.ShouldBe(1);

    binding.Dispose();

    block.Input(new FakeLogicBlock.Input.InputOne(5, 6));

    callStateUpdate.ShouldBe(1);
    callSideEffectHandler.ShouldBe(1);
  }

  [Fact]
  public void WatchesInputs() {
    var block = new FakeLogicBlock();
    var binding = block.Bind();

    var inputOne = 0;
    var inputTwo = 0;

    binding
      .Watch<FakeLogicBlock.Input.InputOne>((input) => inputOne++)
      .Watch<FakeLogicBlock.Input.InputTwo>((input) => inputTwo++);

    block.Input(new FakeLogicBlock.Input.InputOne(1, 2));
    block.Input(new FakeLogicBlock.Input.InputTwo("a", "b"));
    block.Input(new FakeLogicBlock.Input.InputOne(3, 4));

    inputOne.ShouldBe(2);
    inputTwo.ShouldBe(1);
  }

  [Fact]
  public void CatchesExceptions() {
    var block = new FakeLogicBlock();
    var binding = block.Bind();

    var called = false;

    binding.Catch<InvalidOperationException>((e) => {
      called = true;
      e.ShouldBeOfType<InvalidOperationException>();
    });

    block.Input(new FakeLogicBlock.Input.InputError());

    called.ShouldBeTrue();
  }

  [Fact]
  public void CanBeMocked() {
    var logic = new Mock<FakeLogicBlock>((Exception e) => { });

    var binding = new Mock<FakeLogicBlock.IBinding>();

    var input = new FakeLogicBlock.Input.InputOne(1, 2);
    var state = new FakeLogicBlock.State.StateA(1, 2);

    logic.Setup(logic => logic.Bind()).Returns(binding.Object);
    logic.Setup(logic => logic.Input(input)).Returns(state);

    logic.Object.Bind().ShouldBe(binding.Object);
    logic.Object.Input(input).ShouldBe(state);
  }

  [Fact]
  public void Finalizes() {
    // Weak reference has to be created and cleared from a static function
    // or else the GC won't ever collect it :P
    var weakRef = CreateWeakFakeLogicBlockBindingReference();
    Utils.ClearWeakReference(weakRef);
  }

  public static WeakReference CreateWeakFakeLogicBlockBindingReference() =>
    new(new FakeLogicBlock().Bind());
}
