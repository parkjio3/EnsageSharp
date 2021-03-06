using System.Linq;
using System.Threading.Tasks;
using Ensage;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;
using Ensage.SDK.Extensions;
using Ensage.SDK.Helpers;
using Ensage.SDK.Menu;
using AbilityId = Ensage.Common.Enums.AbilityId;

namespace InvokerAnnihilationCrappa.Features
{
    public class AutoGhostWalk
    {
        private readonly Config _main;

        public AutoGhostWalk(Config main)
        {
            _main = main;
            var panel = main.Factory.Menu("Ghost Walk Key");
            Enable = panel.Item("Enable", false);
            MinHealth = panel.Item("Min Health for auto ghost walk", new Slider(15, 1, 100));
            MinUnits = panel.Item("Min Units for auto ghost walk", new Slider(3, 1, 5));
            Range = panel.Item("Range", new Slider(1100, 500, 2000));
            CustomKey = panel.Item("Ghost walk key", new KeyBind('0'));

            CustomKey.Item.ValueChanged += ItemOnValueChanged;
            if (Enable)
            {
                UpdateManager.BeginInvoke(Callback);
            }

            Enable.Item.ValueChanged += (sender, args) =>
            {
                if (args.GetNewValue<bool>())
                    UpdateManager.BeginInvoke(Callback);
            };

            Player.OnExecuteOrder += (sender, args) =>
            {
                var order = args.OrderId;
                if (order == OrderId.Ability || order == OrderId.AbilityLocation || order == OrderId.AbilityTarget ||
                    order == OrderId.ToggleAbility)
                {
                    var ability = args.Ability.GetAbilityId();
                    if (_main.Invoker.GlobalGhostWalkSleeper.Sleeping)
                        args.Process = false;
                    else if (ability == AbilityId.invoker_ghost_walk)
                        _main.Invoker.GlobalGhostWalkSleeper.Sleep(500);
                    
                }
            };
        }

        public MenuItem<Slider> Range { get; set; }

        public MenuItem<Slider> MinUnits { get; set; }

        public MenuItem<Slider> MinHealth { get; set; }

        private void ItemOnValueChanged(object sender, OnValueChangeEventArgs e)
        {
            if (e.GetNewValue<KeyBind>().Active)
                UpdateManager.BeginInvoke(CustomGhostWalk);
        }

        private async Task TryToInvis()
        {
            var invis = _main.Invoker.GhostWalk;
            if (invis.Ability.CanBeCasted())
            {
                _main.Invoker.Wex.UseAbility();
                _main.Invoker.Wex.UseAbility();
                _main.Invoker.Wex.UseAbility();
                invis.Ability.UseAbility();
                _main.Invoker.GlobalGhostWalkSleeper.Sleep(500);
                await Task.Delay(1000);
            }
            else if (invis.Ability.AbilityState == AbilityState.Ready)
            {
                if (_main.SmartInvoke)
                {
                    await _main.Invoker.InvokeAsync(_main.Invoker.GhostWalk);
                }
                else
                {
                    _main.Invoker.Invoke(_main.Invoker.GhostWalk);
                }
            }
        }

        private async void CustomGhostWalk()
        {
            while (CustomKey)
            {
                await TryToInvis();
                await Task.Delay(100);
            }
        }

        private async void Callback()
        {
            while (Enable)
            {
                var me = _main.Invoker.Owner;
                if (me.IsAlive)
                {
                    var health = me.HealthPercent();
                    if (health * 100 <= MinHealth)
                    {
                        var enemyHeroes =
                            EntityManager<Hero>.Entities.Count(
                                x =>
                                    x.IsAlive && !x.IsIllusion && !x.IsAlly(me) &&
                                    x.IsInRange(me, Range));
                        if (enemyHeroes >= MinUnits)
                        {
                            await TryToInvis();
                        }
                    }
                }
                await Task.Delay(200);
            }
        }

        public MenuItem<KeyBind> CustomKey { get; set; }

        public MenuItem<bool> Enable { get; set; }
    }
}