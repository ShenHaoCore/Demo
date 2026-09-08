namespace Demo.Algorithms.Tests;

public class AlgorithmTests
{
    [Fact]
    public void TwoSumSorted_找到一对()
    {
        var nums = new[] { 1, 2, 4, 7, 11, 15 };
        var result = Algorithms.TwoSumSorted(nums, 13);
        Assert.NotNull(result);
        Assert.Equal((1, 4), result); // 2 + 11
        Assert.Equal(13, nums[result.Value.Left] + nums[result.Value.Right]);
    }

    [Fact]
    public void TwoSumSorted_不存在则返回空()
    {
        Assert.Null(Algorithms.TwoSumSorted([1, 2, 3], 100));
    }

    [Fact]
    public void ReverseList_反转链表()
    {
        var head = BuildList(1, 2, 3, 4);
        var reversed = Algorithms.ReverseList(head);
        Assert.Equal(new[] { 4, 3, 2, 1 }, ToArray(reversed));
    }

    [Fact]
    public void ReverseList_空链表()
    {
        Assert.Null(Algorithms.ReverseList(null));
    }

    [Fact]
    public void HasCycle_有环()
    {
        var a = new ListNode(1);
        var b = new ListNode(2);
        var c = new ListNode(3);
        a.Next = b;
        b.Next = c;
        c.Next = b;
        Assert.True(Algorithms.HasCycle(a));
    }

    [Fact]
    public void HasCycle_无环()
    {
        Assert.False(Algorithms.HasCycle(BuildList(1, 2, 3)));
        Assert.False(Algorithms.HasCycle(null));
    }

    [Fact]
    public void LevelOrder_层序遍历()
    {
        //     1
        //    / \
        //   2   3
        //  / \
        // 4   5
        var root = new TreeNode(1)
        {
            Left = new TreeNode(2) { Left = new TreeNode(4), Right = new TreeNode(5) },
            Right = new TreeNode(3)
        };

        var levels = Algorithms.LevelOrder(root);
        Assert.Equal(3, levels.Count);
        Assert.Equal(new[] { 1 }, levels[0]);
        Assert.Equal(new[] { 2, 3 }, levels[1]);
        Assert.Equal(new[] { 4, 5 }, levels[2]);
    }

    [Fact]
    public void Preorder_前序遍历()
    {
        var root = new TreeNode(1)
        {
            Left = new TreeNode(2) { Left = new TreeNode(4), Right = new TreeNode(5) },
            Right = new TreeNode(3)
        };

        Assert.Equal(new[] { 1, 2, 4, 5, 3 }, Algorithms.Preorder(root));
    }

    private static ListNode BuildList(params int[] values)
    {
        var dummy = new ListNode(0);
        var current = dummy;
        foreach (var v in values)
        {
            current.Next = new ListNode(v);
            current = current.Next;
        }

        return dummy.Next!;
    }

    private static int[] ToArray(ListNode? head)
    {
        var list = new List<int>();
        while (head is not null)
        {
            list.Add(head.Value);
            head = head.Next;
        }

        return list.ToArray();
    }
}
