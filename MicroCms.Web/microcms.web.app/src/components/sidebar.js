import React from 'react';
import GlobalConfig from '../config';
import { ApiHelper } from './apihelper';

export function Sidebar(props){
    return (<nav id="sidebarMenu" className="col-md-3 col-lg-2 d-md-block bg-light sidebar collapse">
    <div className="position-sticky pt-3 sidebar-sticky">
        <ul className="nav flex-column">
            {props.links}
        </ul>
    </div></nav>)
}

export class NavigationLink extends React.Component{
    constructor(props) {
        super(props);
        this.state = {
          link: this.props.link,
          childLinks:[],
          expanded:false
        };
      }
      fetchChildItems(e){
        if(!this.state.expanded)
        {
            let id= this.state.link.id;
            ApiHelper.getJson( GlobalConfig.BaseUrl+GlobalConfig.ChildItemsApiPath+"?id="+id).then(res=>{
                this.setState({
                    childLinks: res.data,
                expanded: true});
            });
        }else{
            this.setState({
                childLinks: [],
            expanded: false})
        }
        e.stopPropagation();
      }
    render(){
        this.state.link=this.props.link;
    return (
        <li className="nav-item" key={this.props.link.id} >
        
            <a className="nav-link" aria-current="page" href="#">
            <span className='tree-btn'  onClick={(e)=>this.fetchChildItems(e)}>
                {this.state.expanded?"-":"+"} 
            </span>
            <span onClick={()=>this.props.onClick(this.state.link.id)}>
                 {this.state.link.name}
            </span>
                
            </a>
        <ul className="child-link">
        {
            this.state.childLinks.map((link,index)=>
            {
                return (<NavigationLink key={index} onClick={(id)=>this.props.onClick(id)} link={link}/>);
            })
        }
        </ul>

      </li>);
    };
    }