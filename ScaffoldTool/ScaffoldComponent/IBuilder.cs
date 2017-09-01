namespace ScaffoldTool.ScaffoldComponent
{
    //
    // 摘要：
    //     建造者抽象接口
    interface IBuilder<T>
    {
        T Create();
    }
}
