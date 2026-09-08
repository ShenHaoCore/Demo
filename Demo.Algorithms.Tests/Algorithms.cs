namespace Demo.Algorithms.Tests;

/// <summary>自包含算法实现，供单元测试使用。</summary>
public static class Algorithms
{
    /// <summary>有序数组两数之和（双指针），返回 0-based 下标；找不到返回 null。</summary>
    public static (int Left, int Right)? TwoSumSorted(int[] nums, int target)
    {
        var left = 0;
        var right = nums.Length - 1;
        while (left < right)
        {
            var sum = nums[left] + nums[right];
            if (sum == target)
                return (left, right);
            if (sum < target)
                left++;
            else
                right--;
        }

        return null;
    }

    public static ListNode? ReverseList(ListNode? head)
    {
        ListNode? prev = null;
        var current = head;
        while (current is not null)
        {
            var next = current.Next;
            current.Next = prev;
            prev = current;
            current = next;
        }

        return prev;
    }

    public static bool HasCycle(ListNode? head)
    {
        var slow = head;
        var fast = head;
        while (fast?.Next is not null)
        {
            slow = slow!.Next;
            fast = fast.Next.Next;
            if (ReferenceEquals(slow, fast))
                return true;
        }

        return false;
    }

    public static IList<IList<int>> LevelOrder(TreeNode? root)
    {
        var result = new List<IList<int>>();
        if (root is null)
            return result;

        var queue = new Queue<TreeNode>();
        queue.Enqueue(root);
        while (queue.Count > 0)
        {
            var count = queue.Count;
            var level = new List<int>(count);
            for (var i = 0; i < count; i++)
            {
                var node = queue.Dequeue();
                level.Add(node.Value);
                if (node.Left is not null) queue.Enqueue(node.Left);
                if (node.Right is not null) queue.Enqueue(node.Right);
            }

            result.Add(level);
        }

        return result;
    }

    public static IList<int> Preorder(TreeNode? root)
    {
        var result = new List<int>();
        WalkPreorder(root, result);
        return result;
    }

    private static void WalkPreorder(TreeNode? node, IList<int> result)
    {
        if (node is null) return;
        result.Add(node.Value);
        WalkPreorder(node.Left, result);
        WalkPreorder(node.Right, result);
    }
}

public sealed class ListNode(int value)
{
    public int Value { get; } = value;
    public ListNode? Next { get; set; }
}

public sealed class TreeNode(int value)
{
    public int Value { get; } = value;
    public TreeNode? Left { get; set; }
    public TreeNode? Right { get; set; }
}
