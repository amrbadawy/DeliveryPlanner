using FluentValidation;
using FluentValidation.Results;
using MediatR;
using SoftwareDeliveryPlanner.Application.Behaviors;

namespace SoftwareDeliveryPlanner.Tests;

// ---------------------------------------------------------------------------
// Fake request / response types used only in these tests
// ---------------------------------------------------------------------------

file sealed record TestRequest(string Value) : IRequest<string>;

file sealed class AlwaysValidValidator : AbstractValidator<TestRequest>
{
    public AlwaysValidValidator()
    {
        RuleFor(r => r.Value).NotEmpty();
    }
}

file sealed class AlwaysFailValidator : AbstractValidator<TestRequest>
{
    public AlwaysFailValidator()
    {
        RuleFor(r => r.Value).Must(_ => false).WithMessage("Intentional failure");
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

public class ValidationBehaviorTests
{
    private static RequestHandlerDelegate<string> NextReturning(string value) =>
        _ => Task.FromResult(value);

    #region No validators registered

    [Fact]
    public async Task Handle_NoValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>(
            validators: Enumerable.Empty<IValidator<TestRequest>>());

        var result = await behavior.Handle(
            new TestRequest("hello"),
            NextReturning("ok"),
            CancellationToken.None);

        Assert.Equal("ok", result);
    }

    #endregion

    #region Passing validators

    [Fact]
    public async Task Handle_AllValidatorsPass_CallsNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>(
            validators: [new AlwaysValidValidator()]);

        var result = await behavior.Handle(
            new TestRequest("hello"),
            NextReturning("success"),
            CancellationToken.None);

        Assert.Equal("success", result);
    }

    [Fact]
    public async Task Handle_MultiplePassingValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>(
            validators: [new AlwaysValidValidator(), new AlwaysValidValidator()]);

        var result = await behavior.Handle(
            new TestRequest("hello"),
            NextReturning("success"),
            CancellationToken.None);

        Assert.Equal("success", result);
    }

    #endregion

    #region Failing validators

    [Fact]
    public async Task Handle_OneFailingValidator_ThrowsValidationException()
    {
        var behavior = new ValidationBehavior<TestRequest, string>(
            validators: [new AlwaysFailValidator()]);

        await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(
                new TestRequest("anything"),
                NextReturning("should not reach"),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_FailingValidator_ExceptionContainsErrors()
    {
        var behavior = new ValidationBehavior<TestRequest, string>(
            validators: [new AlwaysFailValidator()]);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(
                new TestRequest("x"),
                NextReturning("n/a"),
                CancellationToken.None));

        Assert.NotEmpty(ex.Errors);
        Assert.Contains(ex.Errors, e => e.ErrorMessage == "Intentional failure");
    }

    [Fact]
    public async Task Handle_MixedValidators_ThrowsWhenAnyFails()
    {
        // One passing, one failing — should still throw
        var behavior = new ValidationBehavior<TestRequest, string>(
            validators: [new AlwaysValidValidator(), new AlwaysFailValidator()]);

        await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(
                new TestRequest("hello"),
                NextReturning("n/a"),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_FailingValidator_NextIsNeverCalled()
    {
        bool nextCalled = false;

        RequestHandlerDelegate<string> trackingNext = _ =>
        {
            nextCalled = true;
            return Task.FromResult("done");
        };

        var behavior = new ValidationBehavior<TestRequest, string>(
            validators: [new AlwaysFailValidator()]);

        await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(new TestRequest("x"), trackingNext, CancellationToken.None));

        Assert.False(nextCalled);
    }

    #endregion

    #region Cancellation

    [Fact]
    public async Task Handle_CancelledToken_ThrowsOperationCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var behavior = new ValidationBehavior<TestRequest, string>(
            validators: [new AlwaysValidValidator()]);

        // next throws OperationCanceledException when already cancelled
        RequestHandlerDelegate<string> cancellingNext = _ =>
            Task.FromCanceled<string>(cts.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            behavior.Handle(new TestRequest("hello"), cancellingNext, cts.Token));
    }

    #endregion
}
