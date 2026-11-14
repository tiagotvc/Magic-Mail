using System.Collections.Generic;
using System.Linq;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Level;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    // Shared authoring contract: one anchor and three references, never four independent safes.
    public static class SafeFootprint
    {
        public static bool Inside(FieldData field,int col,int row) => field!=null && col>=0 && row>=0 && col<field.maxCols && row<field.maxRows;
        public static bool Has(FieldData field,int col,int row) => Inside(field,col,row) &&
            field.levelSquares[row*field.maxCols+col].blocks.Any(b=>b.squareType==SquareTypes.Safe);
        public static Vector2Int Origin(FieldData field,int col,int row) =>
            field.levelSquares[row*field.maxCols+col].blocks.First(b=>b.squareType==SquareTypes.Safe).originalPos;
        public static bool TryPaint(FieldData field,int col,int row,out string error)
        {
            error=null;
            for(int y=row;y<row+2;y++)for(int x=col;x<col+2;x++)
            {
                if(!Inside(field,x,y)){error="O cofre ocupa 2×2 e não cabe nesta borda.";return false;}
                var block=field.levelSquares[y*field.maxCols+x];
                if(block.block==SquareTypes.NONE || block.blocks.Any(b=>b.squareType==SquareTypes.NONE))
                {error="O cofre 2×2 não pode cobrir um buraco.";return false;}
                if(block.blocks.Any(b=>b.squareType!=SquareTypes.EmptySquare && b.squareType!=SquareTypes.SugarSquare) ||
                    block.obstacle!=SquareTypes.NONE)
                {error="O cofre precisa de quatro células sem outro obstáculo. O piso de grama pode permanecer.";return false;}
                if(block.isEnterTeleport || block.teleportCoordinatesLinked.x>=0 || block.teleportCoordinatesLinkedBack.x>=0)
                {error="O cofre não pode cobrir um teleporte.";return false;}
            }
            var origin=new Vector2Int(col,row);
            for(int y=row;y<row+2;y++)for(int x=col;x<col+2;x++)
            {
                var block=field.levelSquares[y*field.maxCols+x];
                block.blocks.Add(new SquareTypeLayer {squareType=SquareTypes.Safe,originalPos=origin,
                    size=new Vector2Int(2,2),anotherSquare=x!=col || y!=row});
                block.obstacle=SquareTypes.Safe;block.obstacleLayer=1;
            }
            field.layers=Mathf.Max(field.layers,field.levelSquares.Max(b=>b.blocks.Count));
            return true;
        }
        public static bool Erase(FieldData field,int col,int row)
        {
            if(!Has(field,col,row))return false;
            var origin=Origin(field,col,row);
            for(int y=origin.y;y<origin.y+2;y++)for(int x=origin.x;x<origin.x+2;x++)
            {
                if(!Inside(field,x,y))continue;
                var block=field.levelSquares[y*field.maxCols+x];
                block.blocks.RemoveAll(b=>b.squareType==SquareTypes.Safe && b.originalPos==origin);
                if(block.obstacle==SquareTypes.Safe){block.obstacle=SquareTypes.NONE;block.obstacleLayer=1;}
                if(block.blocks.Count==0)block.blocks.Add(new SquareTypeLayer{squareType=SquareTypes.EmptySquare});
                block.MergeEmptySquares();
            }
            return true;
        }
        public static bool Validate(FieldData field,out string error)
        {
            error=null;
            for(int row=0;row<field.maxRows;row++)for(int col=0;col<field.maxCols;col++)
            {
                if(!Has(field,col,row))continue;
                var origin=Origin(field,col,row);
                if(col<origin.x || col>origin.x+1 || row<origin.y || row>origin.y+1)
                {error="Referência de cofre fora do seu grupo 2×2.";return false;}
                for(int y=origin.y;y<origin.y+2;y++)for(int x=origin.x;x<origin.x+2;x++)
                {
                    if(!Has(field,x,y)){error="Cofre incompleto: repinte o grupo 2×2 inteiro.";return false;}
                    var layer=field.levelSquares[y*field.maxCols+x].blocks.First(b=>b.squareType==SquareTypes.Safe);
                    if(field.levelSquares[y*field.maxCols+x].blocks.Any(b=>b.squareType!=SquareTypes.EmptySquare &&
                        b.squareType!=SquareTypes.SugarSquare && b.squareType!=SquareTypes.Safe))
                    {error="Há outro obstáculo sobreposto ao cofre. Remova a sobreposição antes de salvar.";return false;}
                    if(layer.originalPos!=origin || layer.size!=new Vector2Int(2,2) || layer.anotherSquare!=(x!=origin.x || y!=origin.y))
                    {error="As quatro células do cofre não apontam para a mesma origem.";return false;}
                }
            }
            return true;
        }
        public static int Count(LevelData data) => data?.fields?.Sum(field=>field.levelSquares.Count(block=>
            block.blocks.Any(b=>b.squareType==SquareTypes.Safe && !b.anotherSquare))) ?? 0;
    }
}
