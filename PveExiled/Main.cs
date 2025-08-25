using Exiled;
using Exiled.API.Features;
using Exiled.Events;
using System;

using ServerEvents = Exiled.Events.Handlers.Server;

public class Main : Plugin<Config>
{
    public override string Name { get; } = "PvE";
    public override string Author { get; } = "A3indae";
    public override Version Version { get; } = new Version(3,0);

    RoundHandler roundHandler;
    public override void OnEnabled()
    {
        roundHandler = new RoundHandler();
        ServerEvents.RoundStarted += roundHandler.OnRoundStarted;
        ServerEvents.EndingRound += roundHandler.OnEndingRound;

    }
    public override void OnDisabled()
    {
        ServerEvents.RoundStarted -= roundHandler.OnRoundStarted;
        ServerEvents.EndingRound -= roundHandler.OnEndingRound;
        roundHandler.OnEndingRound();
        roundHandler = null;
    }
}