using System;
using UnityEngine;

namespace RoyalAves.Meta.WorldMap
{
    [Serializable] public struct WorldMapPiecePose
    {
        public string id;
        public WorldMapEffectPose pose;
        public static WorldMapEffectPose Find(WorldMapPiecePose[] pieces,string id)
        {
            if(pieces!=null)foreach(var p in pieces)if(p.id==id)return p.pose;return default;
        }
        public static WorldMapPiecePose[] Set(WorldMapPiecePose[] pieces,string id,WorldMapEffectPose pose)
        {
            int index=-1;if(pieces!=null)for(int i=0;i<pieces.Length;i++)if(pieces[i].id==id){index=i;break;}
            var copy=new WorldMapPiecePose[(pieces?.Length??0)+(index<0?1:0)];if(pieces!=null)Array.Copy(pieces,copy,pieces.Length);
            copy[index<0?copy.Length-1:index]=new WorldMapPiecePose{id=id,pose=pose};return copy;
        }
    }
    public sealed class WorldMapEffectPiece : MonoBehaviour
    {
        public string Id {get;private set;}
        public string Label {get;private set;}
        Vector3 home,homeScale;Quaternion homeRotation;
        public void Init(string id,string label)
        {
            Id=id;Label=label;home=transform.localPosition;homeScale=transform.localScale;homeRotation=transform.localRotation;
        }
        public void Apply(WorldMapPiecePose[] values,float height)
        {
            var pose=WorldMapPiecePose.Find(values,Id);
            transform.localPosition=home+new Vector3(pose.offset.x*height,pose.offset.y*height,0);
            transform.localRotation=Quaternion.Euler(0,0,pose.rotation)*homeRotation;
            var scale=pose.EffectiveScale;transform.localScale=Vector3.Scale(homeScale,new Vector3(scale.x,scale.y,1));
            gameObject.SetActive(pose.Visible(true));
        }
    }
}
