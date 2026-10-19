using System;
using System.IO;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEngine;

public static class MailboxV44Validation
{
    public static void Run()
    {
        try
        {
            var item=Resources.Load<WorldMapLibraryItem>("MapLibrary/Props/caixa-correio-glossy-v44");
            if(item==null||item.sprite==null||item.category!=WorldMapAssetCategory.Props||item.kind!=PropKind.Mailbox)throw new Exception("Mailbox library registration failed");
            var path=AssetDatabase.GetAssetPath(item.sprite);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if(importer.spriteImportMode!=SpriteImportMode.Single||!importer.alphaIsTransparency)throw new Exception("Sprite transparency import incorrect");
            var mesh=WorldMapCards.Get(item.sprite,item.size);if(mesh==null||mesh.vertexCount<4)throw new Exception("Mailbox map card failed");
            Directory.CreateDirectory("C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/mailbox-v44");
            File.WriteAllText("C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/mailbox-v44/validation.txt","PASS: native Unity import, transparent single sprite, Props library resource, mailbox kind and map card mesh. No source scene, layout or season saved.");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
