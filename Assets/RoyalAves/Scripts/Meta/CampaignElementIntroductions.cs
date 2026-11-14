using System.Linq;
using RoyalAves.Gameplay;
using SweetSugar.Scripts.Core;
using UnityEngine;

namespace RoyalAves.Meta
{
    /// <summary>First authored encounters in the 1–200 campaign. Reuses the existing presentation UI.</summary>
    public static class CampaignElementIntroductions
    {
        public sealed class Entry
        {
            public readonly int level; public readonly string title, instruction, resource;
            public Entry(int n,string t,string text,string path){level=n;title=t;instruction=text;resource=path;}
        }
        public static readonly Entry[] Entries = {
            new Entry(1,"Caixas!","Faça combinações ao lado das <color=#AE193F>CAIXAS</color>\nou use especiais para quebrá-las!","Blocks/SolidBlock"),
            new Entry(2,"Foguete!","Combine <color=#AE193F>4 peças em linha</color> para criar um foguete.\nEle limpa uma linha ou coluna inteira!","Items/HORIZONTAL_STRIPED"),
            new Entry(3,"Dinamite!","Combine peças em <color=#AE193F>T ou L</color> para criar uma dinamite.\nUse-a para atingir várias casas ao redor!","Items/PACKAGE"),
            new Entry(4,"Grama!","Faça combinações <color=#AE193F>sobre a GRAMA</color>\nou use especiais para removê-la!","Blocks/SugarSquare"),
            new Entry(6,"Hélice!","Combine <color=#AE193F>4 peças em um quadrado</color> para criar uma hélice.\nEla voa até um alvo e também pode levar outro especial!","Items/MARMALADE"),
            new Entry(8,"Globo de luz!","Combine <color=#AE193F>5 peças em linha</color> para criar um globo.\nCombine-o com uma cor ou com outro especial!","Items/MULTICOLOR"),
            new Entry(11,"Escaninho!","Faça combinações ao lado do <color=#AE193F>ESCANINHO</color> para abrir as portas.\nContinue acertando para entregar todas as cartas!","Blocks/MailCubby"),
            new Entry(21,"Franqueadora!","Faça combinações ao lado da <color=#AE193F>MÁQUINA</color>\nou use especiais para produzir as cartas da meta!","Blocks/FrankingMachine"),
            new Entry(31,"Gelo!","Faça uma combinação com a peça <color=#AE193F>CONGELADA</color>\nou acerte-a com um especial para quebrar o gelo!","Blocks/IceBlock"),
            new Entry(36,"Gelo duplo!","Este gelo tem <color=#AE193F>DUAS CAMADAS</color>.\nAcerte duas vezes para liberar a peça!","Blocks/IceBlock"),
            new Entry(46,"Ovos mágicos!","Faça combinações ao lado dos <color=#AE193F>OVOS</color>\nou use especiais para quebrá-los!","Items/FLYING_LETTER"),
            new Entry(61,"Poções!","Combine ao lado de cada frasco usando a <color=#AE193F>COR DELE</color>,\nou acerte-o com um especial. Esvazie os quatro frascos!","Blocks/RoutePotions"),
            new Entry(76,"Bolsa do carteiro!","Faça combinações ao lado da <color=#AE193F>BOLSA</color> ou use especiais.\nEla fica no lugar e entrega os pergaminhos da meta!","Blocks/BagOpenings"),
            new Entry(91,"Pedra rúnica!","As <color=#AE193F>PEDRAS RÚNICAS</color> resistem às combinações comuns.\nUse especiais ou combine power-ups para quebrá-las!","Blocks/EnchantedStone"),
            new Entry(106,"Tinteiro!","Acerte o <color=#AE193F>TINTEIRO</color> com combinações ao lado ou especiais.\nEle espalha tinta e quebra depois de cinco acertos!","Blocks/Inkwell"),
            new Entry(121,"Cofre!","O <color=#AE193F>COFRE</color> ocupa quatro casas e só recebe dano de especiais.\nAcerte cinco vezes para abri-lo!","Blocks/Safe"),
            new Entry(136,"Sinalizador!","Faça combinações ao lado com a <color=#AE193F>COR ACESA</color>, ou use especiais.\nA cor muda a cada jogada. Três acertos o removem!","Blocks/SortingSignals"),
            new Entry(151,"Fita postal!","A <color=#AE193F>FITA</color> prende a peça. Faça uma combinação nela ou ao lado,\nou use um especial para descolá-la. Ela não é uma meta!","Blocks/PostalTape")
        };
        public static bool Has(int level) => Entries.Any(e=>e.level==level);
        public static Sprite Icon(Entry entry)
        {
            var prefab=Resources.Load<GameObject>(entry.resource);
            if(prefab==null)return null;
            var cubby=prefab.GetComponent<MailCubbyCell>();
            var machine=prefab.GetComponent<FrankingMachineCell>();
            var visual=cubby!=null?cubby.visualPrefab:machine!=null?machine.visualPrefab:null;
            if(visual!=null){var a=visual.GetComponent<MailCubbyAnimation>();if(a!=null&&a.cabinet!=null)return a.cabinet.sprite;}
            var bag=prefab.GetComponent<BagOpeningStation>();
            if(bag!=null&&bag.frames!=null&&bag.frames.Length>0)return bag.frames[0];
            var potions=prefab.GetComponent<RouteCentralCell>();if(potions!=null&&potions.artwork!=null)return potions.artwork;
            return prefab.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.sprite!=null).Select(r=>r.sprite).FirstOrDefault();
        }
        public static void ShowCurrent()
        {
            var manager=LevelManager.THIS;if(manager==null||manager.levelData==null||manager.field==null)return;
            var entry=Entries.FirstOrDefault(e=>e.level==manager.levelData.levelNum);if(entry==null)return;
            var icon=Icon(entry);
            ElementIntroPrototype.Show(icon,entry.title,entry.instruction);
        }
    }
}
