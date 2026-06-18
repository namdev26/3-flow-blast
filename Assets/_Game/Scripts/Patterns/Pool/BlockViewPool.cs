using System.Collections.Generic;
using FlowBlast.Presentation.Block;
using UnityEngine;

namespace FlowBlast.Patterns.Pool
{
    public sealed class BlockViewPool : IObjectPool<BlockView>
    {
        private readonly BlockView prefab;
        private readonly Transform parent;
        private readonly Queue<BlockView> availableItems = new Queue<BlockView>();

        public BlockViewPool(BlockView prefab, Transform parent)
        {
            this.prefab = prefab;
            this.parent = parent;
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                BlockView instance = CreateInstance();
                Release(instance);
            }
        }

        public BlockView Get()
        {
            if (availableItems.Count > 0)
            {
                BlockView item = availableItems.Dequeue();
                item.gameObject.SetActive(true);
                return item;
            }

            return CreateInstance();
        }

        public void Release(BlockView item)
        {
            if (item == null)
            {
                return;
            }

            item.ResetView();
            item.transform.SetParent(parent, false);
            item.gameObject.SetActive(false);
            availableItems.Enqueue(item);
        }

        private BlockView CreateInstance()
        {
            BlockView instance = Object.Instantiate(prefab, parent);
            instance.gameObject.SetActive(false);
            return instance;
        }
    }
}
